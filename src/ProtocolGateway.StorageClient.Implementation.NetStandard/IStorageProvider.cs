using System;
using System.Threading.Tasks;

namespace ProtocolGateway.StorageClient.Implementation
{
    /// <summary>
    /// Interface similar to IoT DeviceClient class
    /// Simulate handling messages through DeviceClient, 
    /// but instead goes to Storage
    /// 
    /// Docs: https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.devices.client.deviceclient
    /// </summary>
    public interface IStorageProvider
    {
        /// <summary>
        /// Sends an event to device hub
        /// </summary>
        /// <param name="messageId"></param>
        /// <param name="to"></param>
        /// <param name="userId"></param>
        /// <param name="messageBody"></param>
        /// <returns></returns>
        Task SendEventAsync(string messageId, string to, string userId, byte[] messageBody);

        /// <summary>
        /// Explicitly open the DeviceClient instance.
        /// </summary>
        /// <returns></returns>
        Task OpenAsync();

        /// <summary>
        /// Receive a message from the device queue with the specified timeout
        /// </summary>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        Task<byte[]> ReceiveAsync(TimeSpan maxValue);

        /// <summary>
        /// Puts a received message back onto the device queue
        /// </summary>
        /// <param name="messageId"></param>
        /// <returns></returns>
        Task AbandonAsync(string messageId);

        /// <summary>
        /// Deletes a received message from the device queue
        /// </summary>
        /// <param name="messageId"></param>
        /// <returns></returns>
        Task CompleteAsync(string messageId);

        /// <summary>
        /// Deletes a received message from the device queue and 
        /// indicates to the server that the message could not 
        /// be processed.
        /// </summary>
        /// <param name="messageId"></param>
        /// <returns></returns>
        Task RejectAsync(string messageId);

        /// <summary>
        /// Close the DeviceClient instance
        /// </summary>
        /// <returns></returns>
        Task CloseAsync();
        
    }
}