using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.ServiceRuntime;
using NLog;
using Newtonsoft.Json.Linq;
using Raven.Client.Document;
using Raven.Client.Extensions;
using Raven.Database.Plugins;
using Raven.Json.Linq;

namespace RavenDb.Bundles.Azure.Hooks
{
    public class DatabaseCreateTrigger : AbstractPutTrigger
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        public override void OnPut(string key, Raven.Json.Linq.RavenJObject document, Raven.Json.Linq.RavenJObject metadata, Raven.Abstractions.Data.TransactionInformation transactionInformation)
        {
            if (key.StartsWith("Raven/Databases/",StringComparison.InvariantCultureIgnoreCase))
            {
                var databaseName    = key.Replace("Raven/Databases/", string.Empty);
                var dataDirectory   = AzureIntegration.GetStoragePathForDatabase(RoleEnvironment.CurrentRoleInstance, databaseName);

                // We have a database creation request ( or update ) 
                RavenJToken settingsToken = null;
                if (document.TryGetValue("Settings", out settingsToken))
                {
                    var settingsObject = settingsToken as RavenJObject;

                    if (settingsObject != null)
                    {
                        settingsObject["Raven/DataDir"] = new RavenJValue(dataDirectory);
                    }
                }
            }

            base.OnPut(key, document, metadata, transactionInformation);
        }

        public override void AfterCommit(string key, RavenJObject document, RavenJObject metadata, Guid etag)
        {
            if (key.StartsWith("Raven/Databases/", StringComparison.InvariantCultureIgnoreCase))
            {
                var databaseName = key.Replace("Raven/Databases/", string.Empty);

                foreach (var targetUrl in RoleEnvironmentUtilities.GetAllInternalEndpointsExceptThis())
                {
                    log.Info("Ensuring database {0} is created on server {1}",databaseName,targetUrl);

                    using (var connection = new DocumentStore() { Url = targetUrl })
                    {
                        connection.Initialize();
                        connection.DatabaseCommands.EnsureDatabaseExists(databaseName);
                    }
                }
            }

            base.AfterCommit(key, document, metadata, etag);
        }
    }
}
