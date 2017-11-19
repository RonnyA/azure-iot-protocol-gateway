// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.ProtocolGateway.StorageClient
{
    using Microsoft.Azure.Devices.ProtocolGateway.Identity;
    using System.Net;

    public sealed class StorageDeviceIdentity : IDeviceIdentity
    {
        string asString;
        string policyName;
        string userName;

        EndPoint clientAddress;
        public StorageDeviceIdentity(string deviceId, string username, EndPoint clientaddress)
        {
            this.Id = deviceId;
            this.userName = username;
            this.clientAddress = clientaddress;
        }

        public string Id { get; }

        public bool IsAuthenticated => true;

        public string PolicyName
        {
            get { return this.policyName; }
            private set
            {
                this.policyName = value;
                this.asString = null;
            }
        }

        public string Secret { get; private set; }


        public static bool TryParse(string clientId, string username, string password, EndPoint clientAddress, out StorageDeviceIdentity identity)
        {
            //TODO: Do database lookup?
            //TODO: Optional - Add validation logic to client id

            identity = new StorageDeviceIdentity(clientId, username, clientAddress);
            return true;
        }
              
        public override string ToString()
        {
            if (this.asString == null)
            {
                string policy = string.IsNullOrEmpty(this.PolicyName) ? "<none>" : this.PolicyName;
                this.asString = $"{this.Id} [StorageClient: PolicyName: {policy} ]";
            }
            return this.asString;
        }
    }
}