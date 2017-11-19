// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.ProtocolGateway
{
    public class AppConfigSettingsProvider : ISettingsProvider
    {
        IAppConfigReader configStrategy;

        public AppConfigSettingsProvider()
        {
#if NETSTANDARD2_0
            this.configStrategy = new ConfigurationExtensionReader();
#else
            this.configStrategy = new ConfigManagerReader();
#endif
        }
        public bool TryGetSetting(string name, out string value)
        {
            return configStrategy.TryGetSetting(name, out value);
        }
    }
}