using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using NLog;
using Raven.Abstractions.Data;
using Raven.Client.Document;
using Raven.Database.Plugins;
using RavenDb.Bundles.Azure.Storage;

namespace RavenDb.Bundles.Azure.Hooks
{
    public class DatabaseDeleteTrigger : AbstractDeleteTrigger
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        [Import(RequiredCreationPolicy = CreationPolicy.Shared)]
        public IStorageProvider StorageProvider { get; set; }

        [Import]
        public IInstanceEnumerator InstanceEnumerator { get; set; }

        public override void AfterCommit(string key)
        {
            const string prefix = "Raven/Databases/";

            if (key.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase))
            {
                var databaseName    = key.Replace(prefix, string.Empty);
                var selfInstance    = InstanceEnumerator.EnumerateInstances().First(i => i.IsSelf);
          
                if (selfInstance.InstanceType == InstanceType.ReadWrite)
                {
                    foreach (var instance in InstanceEnumerator.EnumerateInstances().Where(i => !i.IsSelf))
                    {
                        log.Info("Ensuring database {0} is deleted on instance {1} at {2}", databaseName, instance.Id,
                                 instance.InternalUrl);

                        using (var documentStore = new DocumentStore() {Url = instance.InternalUrl})
                        {
                            documentStore.Initialize();

                            using (var session = documentStore.OpenSession())
                            {
                                var databaseDocument = session.Load<DatabaseDocument>(key);
                                if (databaseDocument != null)
                                {
                                    session.Delete(databaseDocument);
                                    session.SaveChanges();
                                }
                            }
                        }
                    }
                }

                // Should we delete the files really ? On the other hand azure drive maintenance is impossible right now ...
                var directory = StorageProvider.GetDirectoryForDatabase(databaseName);
                if (directory.Exists)
                {
                    log.Info("Deleting directory {0} for database {1}",directory.FullName,databaseName);
                    //directory.Delete(true);
                }
            }

            base.AfterCommit(key);
        }
    }
}
