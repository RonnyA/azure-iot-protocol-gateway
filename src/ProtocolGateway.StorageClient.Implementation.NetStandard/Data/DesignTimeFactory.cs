using Microsoft.Azure.Devices.ProtocolGateway.StorageClient.Implementation.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Devices.ProtocolGateway.StorageClient.Implementation.Data
{

    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DeviceContext>
    {
        public DeviceContext CreateDbContext(string[] args)
        {
            

            var builder = new DbContextOptionsBuilder<DeviceContext>();

            //var connectionString = configuration.GetConnectionString("DefaultConnection");

            builder.UseSqlServer(DeviceContext.DEFAULT_DB_CONNECTION);

            return new DeviceContext(builder.Options);
        }

    }
}

