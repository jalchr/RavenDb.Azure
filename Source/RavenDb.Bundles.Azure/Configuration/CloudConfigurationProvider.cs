using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace RavenDb.Bundles.Azure.Configuration
{
    [Export(typeof(IConfigurationProvider))]
    public class CloudConfigurationProvider : IConfigurationProvider
    {
        public string GetSetting(string key)
        {
            try
            {
                return RoleEnvironment.GetConfigurationSettingValue(key);
            }
            catch (RoleEnvironmentException)
            {
                return null;
            }
        }
    }
}
