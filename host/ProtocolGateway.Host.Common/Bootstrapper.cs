#define USE_STORAGE_GATEWAY
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ProtocolGateway.Host.Common
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using DotNetty.Buffers;
    using DotNetty.Common.Utilities;
    using DotNetty.Codecs.Mqtt;
    using DotNetty.Common.Concurrency;
    using DotNetty.Handlers.Tls;
    using DotNetty.Transport.Bootstrapping;
    using DotNetty.Transport.Channels;
    using DotNetty.Transport.Channels.Sockets;
    using Microsoft.Azure.Devices.ProtocolGateway;
    using Microsoft.Azure.Devices.ProtocolGateway.Identity;
    using Microsoft.Azure.Devices.ProtocolGateway.Instrumentation;
    using Microsoft.Azure.Devices.ProtocolGateway.IotHubClient;
    using Microsoft.Azure.Devices.ProtocolGateway.IotHubClient.Addressing;
    using Microsoft.Azure.Devices.ProtocolGateway.Messaging;
    using Microsoft.Azure.Devices.ProtocolGateway.Mqtt;
    using Microsoft.Azure.Devices.ProtocolGateway.Mqtt.Persistence;
    using Microsoft.Azure.Devices.ProtocolGateway.StorageClient;



    public class Bootstrapper
    {
        const int MqttsPort = 1883; //TODO: Move back to 8883 when enabling TLS
        const int ListenBacklogSize = 200; // connections allowed pending accept
        const int DefaultConnectionPoolSize = 400; // IoT Hub default connection pool size
        static readonly TimeSpan DefaultConnectionIdleTimeout = TimeSpan.FromSeconds(210); // IoT Hub default connection idle timeout

        readonly TaskCompletionSource closeCompletionSource;
        readonly ISettingsProvider settingsProvider;
        readonly Settings settings;
        readonly ISessionStatePersistenceProvider sessionStateManager;
        readonly IQos2StatePersistenceProvider qos2StateProvider;
        readonly IDeviceIdentityProvider authProvider;
        X509Certificate2 tlsCertificate;
        IEventLoopGroup parentEventLoopGroup;
        IEventLoopGroup eventLoopGroup;
        IChannel serverChannel;

#if USE_STORAGE_GATEWAY
        readonly StorageClientSettings StorageClientSettings;
#else
        readonly IotHubClientSettings iotHubClientSettings;
#endif

        readonly IMessageAddressConverter topicNameConverter;

        public Bootstrapper(ISettingsProvider settingsProvider, ISessionStatePersistenceProvider sessionStateManager, IQos2StatePersistenceProvider qos2StateProvider) : 
            this(settingsProvider, sessionStateManager, qos2StateProvider, new ConfigurableMessageAddressConverter())
        {
        }

        public Bootstrapper(ISettingsProvider settingsProvider, ISessionStatePersistenceProvider sessionStateManager, IQos2StatePersistenceProvider qos2StateProvider, List<string> inboundTemplates, List<string> outboundTemplates) : 
            this(settingsProvider, sessionStateManager, qos2StateProvider, new ConfigurableMessageAddressConverter(inboundTemplates ?? new List<string>(), outboundTemplates ?? new List<string>()))
        {
        }

        Bootstrapper(ISettingsProvider settingsProvider, ISessionStatePersistenceProvider sessionStateManager, IQos2StatePersistenceProvider qos2StateProvider, IMessageAddressConverter addressConverter)
        {
            Contract.Requires(settingsProvider != null);
            Contract.Requires(sessionStateManager != null);

            this.closeCompletionSource = new TaskCompletionSource();

            this.settingsProvider = settingsProvider;
            this.settings = new Settings(this.settingsProvider);

#if USE_STORAGE_GATEWAY
            this.StorageClientSettings = new StorageClientSettings(this.settingsProvider);
            this.authProvider = new StorageDeviceIdentityProvider();
#else            
            this.iotHubClientSettings = new IotHubClientSettings(this.settingsProvider);
            this.authProvider = new SasTokenDeviceIdentityProvider();
#endif          

            this.sessionStateManager = sessionStateManager;
            this.qos2StateProvider = qos2StateProvider;
            this.topicNameConverter = addressConverter;
        }

        public Task CloseCompletion => this.closeCompletionSource.Task;

        public async Task RunAsync(X509Certificate2 certificate, int threadCount, CancellationToken cancellationToken)
        {
            Contract.Requires(certificate != null);
            Contract.Requires(threadCount > 0);

            try
            {
                BootstrapperEventSource.Log.Info("Starting", null);

                PerformanceCounters.ConnectionsEstablishedTotal.RawValue = 0;
                PerformanceCounters.ConnectionsCurrent.RawValue = 0;

                this.tlsCertificate = certificate;
                this.parentEventLoopGroup = new MultithreadEventLoopGroup(1);
                this.eventLoopGroup = new MultithreadEventLoopGroup(threadCount);

                ServerBootstrap bootstrap = this.SetupBootstrap();
                BootstrapperEventSource.Log.Info($"Initializing TLS endpoint on port {MqttsPort.ToString()} with certificate {this.tlsCertificate.Thumbprint}.", null);
                this.serverChannel = await bootstrap.BindAsync(IPAddress.Any, MqttsPort);

                this.serverChannel.CloseCompletion.LinkOutcome(this.closeCompletionSource);
                cancellationToken.Register(this.CloseAsync);

                BootstrapperEventSource.Log.Info("Started", null);
            }
            catch (Exception ex)
            {
                BootstrapperEventSource.Log.Error("Failed to start", ex);
                this.CloseAsync();
            }
        }

        async void CloseAsync()
        {
            try
            {
                BootstrapperEventSource.Log.Info("Stopping", null);

                if (this.serverChannel != null)
                {
                    await this.serverChannel.CloseAsync();
                }
                if (this.eventLoopGroup != null)
                {
                    await this.eventLoopGroup.ShutdownGracefullyAsync();
                }

                BootstrapperEventSource.Log.Info("Stopped", null);
            }
            catch (Exception ex)
            {
                BootstrapperEventSource.Log.Warning("Failed to stop cleanly", ex);
            }
            finally
            {
                this.closeCompletionSource.TryComplete();
            }
        }

        ServerBootstrap SetupBootstrap()
        {
#if USE_STORAGE_GATEWAY
            if (this.settings.DeviceReceiveAckCanTimeout && this.StorageClientSettings.MaxPendingOutboundMessages > 1)
#else
            if (this.settings.DeviceReceiveAckCanTimeout && this.iotHubClientSettings.MaxPendingOutboundMessages > 1)
#endif
            {
                throw new InvalidOperationException("Cannot maintain ordering on retransmission with more than 1 allowed pending outbound message.");
            }

            int maxInboundMessageSize = this.settingsProvider.GetIntegerSetting("MaxInboundMessageSize", 256 * 1024);
            int connectionPoolSize = this.settingsProvider.GetIntegerSetting("IotHubClient.ConnectionPoolSize", DefaultConnectionPoolSize);
            TimeSpan connectionIdleTimeout = this.settingsProvider.GetTimeSpanSetting("IotHubClient.ConnectionIdleTimeout", DefaultConnectionIdleTimeout);


#if USE_STORAGE_GATEWAY
            string strDbConnection = "database connection string"; //TODO: Implement
            Func<IDeviceIdentity, Task<IMessagingServiceClient>> deviceClientFactory = StorageClient.PreparePoolFactory(strDbConnection, connectionPoolSize,
                connectionIdleTimeout, StorageClientSettings, PooledByteBufferAllocator.Default); //, this.topicNameConverter);
#else
            string connectionString = this.iotHubClientSettings.IotHubConnectionString;

            Func<IDeviceIdentity, Task<IMessagingServiceClient>> deviceClientFactory = IotHubClient.PreparePoolFactory(connectionString, connectionPoolSize,
                connectionIdleTimeout, this.iotHubClientSettings, PooledByteBufferAllocator.Default, this.topicNameConverter);
#endif

            MessagingBridgeFactoryFunc bridgeFactory = async deviceIdentity => new SingleClientMessagingBridge(deviceIdentity, await deviceClientFactory(deviceIdentity));

            return new ServerBootstrap()
                .Group(this.parentEventLoopGroup, this.eventLoopGroup)
                .Option(ChannelOption.SoBacklog, ListenBacklogSize)
                .ChildOption(ChannelOption.Allocator, PooledByteBufferAllocator.Default)
                .ChildOption(ChannelOption.AutoRead, false)
                .Channel<TcpServerSocketChannel>()
                .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
                {
                    // TODO: Add back when enabling TLS
                    //channel.Pipeline.AddLast(TlsHandler.Server(this.tlsCertificate));
                    channel.Pipeline.AddLast(
                        MqttEncoder.Instance,
                        new MqttDecoder(true, maxInboundMessageSize),
#if USE_STORAGE_GATEWAY
                        new MqttAdapterStorageGateway(
#else
                        new MqttAdapter(
#endif
                            this.settings,
                            this.sessionStateManager,
                            this.authProvider,
                            this.qos2StateProvider,
                            bridgeFactory));
                }));
        }
    }
}