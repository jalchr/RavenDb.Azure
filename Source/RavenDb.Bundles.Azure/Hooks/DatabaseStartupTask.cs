using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.ServiceRuntime;
using NLog;
using Raven.Database.Plugins;
using RavenDb.Bundles.Azure.Diagnostics;
using RavenDb.Bundles.Azure.Replication;
using RavenDb.Bundles.Azure.Storage;

namespace RavenDb.Bundles.Azure.Hooks
{
    public class DatabaseStartupTask : IStartupTask
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        [Import(RequiredCreationPolicy = CreationPolicy.Shared)]
        public IDiagnosticsProvider DiagnosticsProvider { get; set; }

        [Import(RequiredCreationPolicy = CreationPolicy.Shared)]
        public IStorageProvider     StorageProvider     { get; set; }

        [Import]
        public IInstanceEnumerator  InstanceEnumerator { get; set; }
       
        public void Execute(Raven.Database.DocumentDatabase database)
        {
            // First step setup diagnostics:
            DiagnosticsProvider.Initialize();

            // And then storage:
            StorageProvider.Initialize();

            var storageDirectory = StorageProvider.GetDirectoryForDatabase(database.Name);
            log.Info("Setting storage directory for default database to: {0}",storageDirectory.FullName);
            database.Configuration.DataDirectory = storageDirectory.FullName;

            // Setup replication:
            var selfInstance = InstanceEnumerator.EnumerateInstances().First(i => i.IsSelf);
            if (selfInstance.InstanceType == InstanceType.ReadWrite )
            {
                ReplicationUtilities.UpdateReplication(selfInstance,InstanceEnumerator, database);
            }
        }
    }
}
