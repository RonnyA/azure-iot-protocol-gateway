
using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Devices.ProtocolGateway.StorageClient.Implementation.Models
{
    public class OutgoingMessage
    {
        public int ID { get; set; }
        public Device ToDevice { get; set; }
        public string Body { get; set; }
    }
}
