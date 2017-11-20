// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.ProtocolGateway.StorageClient
{
    using System;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Threading.Tasks;
    using DotNetty.Buffers;
    using DotNetty.Common.Utilities;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Exceptions;
    using Microsoft.Azure.Devices.ProtocolGateway.Identity;
    using Microsoft.Azure.Devices.ProtocolGateway.Instrumentation;
    //using Microsoft.Azure.Devices.ProtocolGateway.IotHubClient.Addressing;
    using Microsoft.Azure.Devices.ProtocolGateway.Messaging;
    using Microsoft.Azure.Devices.ProtocolGateway.Mqtt;
    using System.Text;
    using global::ProtocolGateway.StorageClient.Implementation;
    using Microsoft.Azure.Devices.ProtocolGateway.IotHubClient.Addressing;
    using Microsoft.Azure.Devices.ProtocolGateway.StorageClient.Implementation.Data;

    public class StorageClient : IMessagingServiceClient
    {        
        readonly string deviceId;
        readonly StorageClientSettings settings;
        readonly IByteBufferAllocator allocator;
        readonly IMessageAddressConverter messageAddressConverter;
        IMessagingChannel<IMessage> messagingChannel;
        IStorageProvider storageProvider;

        StorageClient(IStorageProvider storageProvider, string deviceId, StorageClientSettings settings, IByteBufferAllocator allocator, IMessageAddressConverter messageAddressConverter)
        {
            //this.StorageClient = deviceClient;
            this.deviceId = deviceId;
            this.settings = settings;
            this.allocator = allocator;
            this.messageAddressConverter = messageAddressConverter;           
            this.storageProvider = storageProvider;

        }

        public static async Task<IMessagingServiceClient> CreateFromConnectionStringAsync(string deviceId, string connectionString,
            int connectionPoolSize, TimeSpan? connectionIdleTimeout, StorageClientSettings settings, IByteBufferAllocator allocator, IMessageAddressConverter messageAddressConverter)
        {
            //int maxPendingOutboundMessages = settings.MaxPendingOutboundMessages;
            //webSocketSettings.PrefetchCount = tcpSettings.PrefetchCount = (uint)maxPendingOutboundMessages;


            IStorageProvider storageProvider = null;

            //DEBUG
            connectionString = DeviceContext.DEFAULT_DB_CONNECTION; 
            
            //DEBUG

            if (connectionPoolSize > 0)
            {
                //var amqpConnectionPoolSettings = new AmqpConnectionPoolSettings
                //{
                //    MaxPoolSize = unchecked ((uint)connectionPoolSize),
                //    Pooling = connectionPoolSize > 0
                //};
                //if (connectionIdleTimeout.HasValue)
                //{
                //    amqpConnectionPoolSettings.ConnectionIdleTimeout = connectionIdleTimeout.Value;
                //}
            }
            storageProvider = new SQLStorageProvider(connectionString,deviceId);
            try
            {
                await storageProvider.OpenAsync();
            }            
            catch (IotHubException ex)
            {
                throw ComposeStorageCommunicationException(ex);
            }

            return new StorageClient(storageProvider, deviceId, settings, allocator, messageAddressConverter);
        }

        /// <summary>
        /// Function to initiate new Storahe processing class pr MQTT device connected
        /// </summary>
        /// <param name="baseConnectionString"></param>
        /// <param name="connectionPoolSize"></param>
        /// <param name="connectionIdleTimeout"></param>
        /// <param name="settings"></param>
        /// <param name="allocator"></param>
        /// <param name="messageAddressConverter"></param>
        /// <returns></returns>
        public static Func<IDeviceIdentity, Task<IMessagingServiceClient>> PreparePoolFactory(string baseConnectionString, int connectionPoolSize,
            TimeSpan? connectionIdleTimeout, StorageClientSettings settings, IByteBufferAllocator allocator, IMessageAddressConverter messageAddressConverter)
        {
            Func<IDeviceIdentity, Task<IMessagingServiceClient>> mqttCommunicatorFactory = deviceIdentity =>
            {
                string connectionString = "";
                // Storage connection string = ? baseConnectionString);
                var identity = (StorageDeviceIdentity)deviceIdentity;

                return CreateFromConnectionStringAsync(identity.Id, connectionString, connectionPoolSize, connectionIdleTimeout, settings, allocator, messageAddressConverter);
            };
            return mqttCommunicatorFactory;
        }

        public int MaxPendingMessages => this.settings.MaxPendingInboundMessages;

        public IMessage CreateMessage(string address, IByteBuffer payload)
        {
            var message = new StorageClientMessage(new Message(payload.IsReadable() ? new ReadOnlyByteBufferStream(payload, false) : null), payload);
            message.Address = address;
            return message;
        }

        public void BindMessagingChannel(IMessagingChannel<IMessage> channel)
        {
            Contract.Requires(channel != null);

            Contract.Assert(this.messagingChannel == null);

            this.messagingChannel = channel;
            this.Receive();
        }

        public async Task SendAsync(IMessage message)
        {
            var clientMessage = (StorageClientMessage)message;
            try
            {
                string address = message.Address;
                //if (this.messageAddressConverter.TryParseAddressIntoMessageProperties(address, message))
                //{
                //    string messageDeviceId;
                //    if (message.Properties.TryGetValue(TemplateParameters.DeviceIdTemplateParam, out messageDeviceId))
                //    {
                //        if (!this.deviceId.Equals(messageDeviceId, StringComparison.Ordinal))
                //        {
                //            throw new InvalidOperationException(
                //                $"Device ID provided in topic name ({messageDeviceId}) does not match ID of the device publishing message ({this.deviceId})");
                //        }
                //        message.Properties.Remove(TemplateParameters.DeviceIdTemplateParam);
                //    }
                //}
                //else
                //{
                //    if (!this.settings.PassThroughUnmatchedMessages)
                //    {
                //        throw new InvalidOperationException($"Topic name `{address}` could not be matched against any of the configured routes.");
                //    }

                //    CommonEventSource.Log.Warning("Topic name could not be matched against any of the configured routes. Falling back to default telemetry settings.", address);
                //    message.Properties[this.settings.ServicePropertyPrefix + MessagePropertyNames.Unmatched] = bool.TrueString;
                //    message.Properties[this.settings.ServicePropertyPrefix + MessagePropertyNames.Subject] = address;
                //}
                //Message iotHubMessage = clientMessage.ToMessage();
                if ((clientMessage.Payload.ReadableBytes > 0) && clientMessage.Payload.IsReadable())
                {
                    byte[] dest = new byte[clientMessage.Payload.ReadableBytes];
                    clientMessage.Payload.ReadBytes(dest);
                    
                    //string dump = ByteBufferUtil.PrettyHexDump(clientMessage.Payload);

                    //ReadBytes(byte[] destination)

                    await this.storageProvider.SendEventAsync(clientMessage.Id, clientMessage.Address, deviceId, dest); //TODO: Extend with other properties if needed
                }
                //await insert message in storage (SQL table, tablestore,++)
                

            }
            catch (Exception ex)
            {
                throw ComposeStorageCommunicationException(ex);
            }
            finally
            {
                clientMessage.Dispose();
            }
        }

        async void Receive()
        {
            Message message = null;
            IByteBuffer messagePayload = null;

            byte[] msgBody;

            try
            {
                while (true)
                {
                    msgBody = await this.storageProvider.ReceiveAsync(TimeSpan.MaxValue);
                    //message = await this.deviceClient.ReceiveAsync(TimeSpan.MaxValue);                    
                    message = new Message(msgBody);

                    if (message == null)
                    {
                        this.messagingChannel.Close(null);
                        return;
                    }
                   

                    if (this.settings.MaxOutboundRetransmissionEnforced && message.DeliveryCount > this.settings.MaxOutboundRetransmissionCount)
                    {
                        await this.RejectAsync(message.LockToken);
                        message.Dispose();
                        continue;
                    }

                    using (Stream payloadStream = message.GetBodyStream())
                    {
                        long streamLength = payloadStream.Length;
                        if (streamLength > int.MaxValue)
                        {
                            throw new InvalidOperationException($"Message size ({streamLength.ToString()} bytes) is too big to process.");
                        }

                        int length = (int)streamLength;
                        messagePayload = this.allocator.Buffer(length, length);
                        await messagePayload.WriteBytesAsync(payloadStream, length);

                        Contract.Assert(messagePayload.ReadableBytes == length);
                    }

                    var msg = new StorageClientMessage(message, messagePayload);
                    msg.Properties[TemplateParameters.DeviceIdTemplateParam] = this.deviceId;
                    string address = "";

                    //if (!this.messageAddressConverter.TryDeriveAddress(msg, out address))
                    //{
                    //    messagePayload.Release();
                    //    await this.RejectAsync(message.LockToken); // todo: fork await
                    //    message.Dispose();
                    //    continue;
                    //}
                    msg.Address = this.deviceId; // address;
                    
                    this.messagingChannel.Handle(msg);

                    message = null; // ownership has been transferred to messagingChannel
                    messagePayload = null;
                }
            }
            catch (IotHubException ex)
            {
                this.messagingChannel.Close(new MessagingException(ex.Message, ex.InnerException, ex.IsTransient, ex.TrackingId));
            }
            catch (Exception ex)
            {
                this.messagingChannel.Close(ex);
            }
            finally
            {
                message?.Dispose();
                if (messagePayload != null)
                {
                    ReferenceCountUtil.SafeRelease(messagePayload);
                }
            }
        }

        public async Task AbandonAsync(string messageId)
        {
            try
            {
                await this.storageProvider.AbandonAsync(messageId);                
            }
            catch (IotHubException ex)
            {
                throw ComposeStorageCommunicationException(ex);
            }
        }

        public async Task CompleteAsync(string messageId)
        {
            try
            {
                await this.storageProvider.CompleteAsync(messageId);
            }
            catch (IotHubException ex)
            {
                throw ComposeStorageCommunicationException(ex);
            }
        }

        public async Task RejectAsync(string messageId)
        {
            try
            {
                await this.storageProvider.RejectAsync(messageId);
            }
            catch (IotHubException ex)
            {
                throw ComposeStorageCommunicationException(ex);
            }
        }

        public async Task DisposeAsync(Exception cause)
        {
            try
            {
                await this.storageProvider.CloseAsync();
            }
            catch (Exception ex)
            {
                throw ComposeStorageCommunicationException(ex);
            }
        }

        //internal static IAuthenticationMethod DeriveAuthenticationMethod(IAuthenticationMethod currentAuthenticationMethod, StorageDeviceIdentity deviceIdentity)
        //{
        //    switch (deviceIdentity.Scope)
        //    {
        //        case AuthenticationScope.None:
        //            var policyKeyAuth = currentAuthenticationMethod as DeviceAuthenticationWithSharedAccessPolicyKey;
        //            if (policyKeyAuth != null)
        //            {
        //                return new DeviceAuthenticationWithSharedAccessPolicyKey(deviceIdentity.Id, policyKeyAuth.PolicyName, policyKeyAuth.Key);
        //            }
        //            var deviceKeyAuth = currentAuthenticationMethod as DeviceAuthenticationWithRegistrySymmetricKey;
        //            if (deviceKeyAuth != null)
        //            {
        //                return new DeviceAuthenticationWithRegistrySymmetricKey(deviceIdentity.Id, deviceKeyAuth.DeviceId);
        //            }
        //            var deviceTokenAuth = currentAuthenticationMethod as DeviceAuthenticationWithToken;
        //            if (deviceTokenAuth != null)
        //            {
        //                return new DeviceAuthenticationWithToken(deviceIdentity.Id, deviceTokenAuth.Token);
        //            }
        //            throw new InvalidOperationException("");
        //        case AuthenticationScope.SasToken:
        //            return new DeviceAuthenticationWithToken(deviceIdentity.Id, deviceIdentity.Secret);
        //        case AuthenticationScope.DeviceKey:
        //            return new DeviceAuthenticationWithRegistrySymmetricKey(deviceIdentity.Id, deviceIdentity.Secret);
        //        case AuthenticationScope.HubKey:
        //            return new DeviceAuthenticationWithSharedAccessPolicyKey(deviceIdentity.Id, deviceIdentity.PolicyName, deviceIdentity.Secret);
        //        default:
        //            throw new InvalidOperationException("Unexpected AuthenticationScope value: " + deviceIdentity.Scope);
        //    }
        //}

        static MessagingException ComposeStorageCommunicationException(Exception ex)
        {
            return new MessagingException(ex.Message, ex.InnerException);
        }
    }
}