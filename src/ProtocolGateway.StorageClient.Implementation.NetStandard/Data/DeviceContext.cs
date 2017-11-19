using Microsoft.Azure.Devices.ProtocolGateway.StorageClient.Implementation.Models;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Azure.Devices.ProtocolGateway.StorageClient.Implementation.Data
{
    public class DeviceContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public DeviceContext(DbContextOptions<DeviceContext> options) : base(options)
        {
        }

        public DbSet<Device> Devices { get; set; }
        public DbSet<IncommingMessage> Incomming { get; set; }
        public DbSet<OutgoingMessage> Outgoing { get; set; }
    }
}