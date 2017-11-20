using System;
using System.Threading.Tasks;

namespace ProtocolGateway.StorageClient.Implementation
{
    public interface IStorageProvider
    {
        Task SendEventAsync(string messageId, string to, string userId, byte[] messageBody);
        Task OpenAsync();        
        Task<byte[]> ReceiveAsync(TimeSpan maxValue);
        Task AbandonAsync(string messageId);
        Task CompleteAsync(string messageId);
        Task RejectAsync(string messageId);
        Task CloseAsync();
        
    }
}