// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.ProtocolGateway.StorageClient
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.ProtocolGateway.Identity;

    
    public sealed class StorageDeviceIdentityProvider : IDeviceIdentityProvider
    {
        public Task<IDeviceIdentity> GetAsync(string clientId, string username, string password, EndPoint clientAddress)
        {
            StorageDeviceIdentity deviceIdentity;
            if (!StorageDeviceIdentity.TryParse(username,username,password, clientAddress, out deviceIdentity))
            {
                return Task.FromResult(UnauthenticatedDeviceIdentity.Instance);
            }            
            return Task.FromResult<IDeviceIdentity>(deviceIdentity);
        }
    }
}