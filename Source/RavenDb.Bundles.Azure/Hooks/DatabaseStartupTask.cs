using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.ServiceRuntime;
using Raven.Database.Plugins;

namespace RavenDb.Bundles.Azure.Hooks
{
    public class DatabaseStartupTask : IStartupTask 
    {
        public void Execute(Raven.Database.DocumentDatabase database)
        {
            database.Configuration.DataDirectory = AzureIntegration.GetStoragePathForDatabase(RoleEnvironment.CurrentRoleInstance, database.Name);
        }
    }
}
