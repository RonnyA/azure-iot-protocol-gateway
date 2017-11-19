using System;
using System.Threading.Tasks;

namespace ProtocolGateway.StorageClient.Implementation
{
    public class SQLStorageProvider : IStorageProvider
    {
        private string connectionString;

        public SQLStorageProvider(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public Task OpenAsync()
        {
            throw new NotImplementedException();
        }
    }
}
