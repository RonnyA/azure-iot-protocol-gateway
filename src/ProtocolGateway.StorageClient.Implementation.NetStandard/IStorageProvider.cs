using System.Threading.Tasks;

namespace ProtocolGateway.StorageClient.Implementation
{
    public interface IStorageProvider
    {
        Task OpenAsync();
    }
}