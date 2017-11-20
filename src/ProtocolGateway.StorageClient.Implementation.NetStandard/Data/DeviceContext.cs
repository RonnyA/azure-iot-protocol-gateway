using Microsoft.Azure.Devices.ProtocolGateway.StorageClient.Implementation.Models;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Azure.Devices.ProtocolGateway.StorageClient.Implementation.Data
{
    
    public class DeviceContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public const string DEFAULT_DB_CONNECTION = "Server=(localdb)\\mssqllocaldb;Database=MQTT_DeviceDB;Trusted_Connection=True;MultipleActiveResultSets=true";

        private DbContextOptionsBuilder options;

        public string dbcontextConnectionString { get; set; }


        public DeviceContext(DbContextOptions<DeviceContext> options) : base(options)
        {
            dbcontextConnectionString = DEFAULT_DB_CONNECTION;
        }


        public DbSet<Device> Devices { get; set; }
        public DbSet<IncommingMessage> Incomming { get; set; }
        public DbSet<OutgoingMessage> Outgoing { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(dbcontextConnectionString);
        }
    }
}