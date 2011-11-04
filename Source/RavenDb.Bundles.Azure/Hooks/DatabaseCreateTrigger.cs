using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.ServiceRuntime;
using NLog;
using Newtonsoft.Json.Linq;
using Raven.Abstractions.Replication;
using Raven.Client.Document;
using Raven.Client.Extensions;
using Raven.Database.Plugins;
using Raven.Json.Linq;
using RavenDb.Bundles.Azure.Replication;
using RavenDb.Bundles.Azure.Storage;

namespace RavenDb.Bundles.Azure.Hooks
{
    public class DatabaseCreateTrigger : AbstractPutTrigger
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        [Import(RequiredCreationPolicy = CreationPolicy.Shared)]
        public IStorageProvider     StorageProvider { get; set; }

        [Import]
        public IInstanceEnumerator  InstanceEnumerator { get; set; }

        public override void OnPut(string key, RavenJObject document, RavenJObject metadata, Raven.Abstractions.Data.TransactionInformation transactionInformation)
        {
            string databaseName = null;

            if (TryGetDatabase(key, out databaseName))
            {
                var dataDirectory = StorageProvider.GetDirectoryForDatabase(databaseName);

                RavenJToken settingsToken = null;

                if (document.TryGetValue("Settings", out settingsToken))
                {
                    var settingsObject = settingsToken as RavenJObject;

                    if (settingsObject != null)
                    {
                        settingsObject["Raven/DataDir"] = new RavenJValue(dataDirectory.FullName);
                    }
                }
            }

            base.OnPut(key, document, metadata, transactionInformation);
        }

        public override void AfterCommit(string key, RavenJObject document, RavenJObject metadata, Guid etag)
        {
            string databaseName = null;

            if (TryGetDatabase(key, out databaseName))
            {
                var selfInstance = InstanceEnumerator.EnumerateInstances().First(i => i.IsSelf);

                if (selfInstance.InstanceType == InstanceType.ReadWrite)
                {
                    // Ensure database exists:
                    foreach (var instance in InstanceEnumerator.EnumerateInstances().Where(i => !i.IsSelf))
                    {
                        using (var documentStore = new DocumentStore() {Url = instance.InternalUrl})
                        {
                            log.Info("Ensuring database {0} exists on instance {1} at {2}", databaseName, instance.Id,
                                     instance.InternalUrl);

                            documentStore.Initialize();
                            documentStore.DatabaseCommands.EnsureDatabaseExists(databaseName);
                        }
                    }

                    ReplicationUtilities.UpdateReplication(selfInstance,InstanceEnumerator, databaseName);
                }
            }

            base.AfterCommit(key, document, metadata, etag);
        }

        private static bool TryGetDatabase( string key,out string databaseName)
        {
            const string prefix = "Raven/Databases/";

            if (key.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase))
            {
                databaseName = key.Replace(prefix, string.Empty);
                return true;
            }

            databaseName = null;
            return false;
        }
    }
}
