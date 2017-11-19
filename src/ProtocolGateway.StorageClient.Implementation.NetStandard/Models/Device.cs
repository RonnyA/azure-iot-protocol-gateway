using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Devices.ProtocolGateway.StorageClient.Implementation.Models
{
    public class Device
    {
        public int ID { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
