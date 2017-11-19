
using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Devices.ProtocolGateway.StorageClient.Implementation.Models
{
    public class IncommingMessage
    {
        public int ID { get; set; }
        public Device FromDevice { get; set; }
        public string Body { get; set; }        
    }
}
