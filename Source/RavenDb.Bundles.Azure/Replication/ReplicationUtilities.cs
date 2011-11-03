using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using Raven.Abstractions.Replication;
using Raven.Client.Document;

namespace RavenDb.Bundles.Azure.Replication
{
    public static class ReplicationUtilities
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        public static void UpdateReplication( IInstanceEnumerator instanceEnumerator,string databaseName )
        {
            var selfInstance = instanceEnumerator.EnumerateInstances().First(i => i.IsSelf);

            // Setup replication:
            if (selfInstance.InstanceType == InstanceType.ReadWrite)
            {
                using (var documentStore = new DocumentStore() { Url = selfInstance.InternalUrl })
                {
                    log.Info("Ensuring database {0} is replicated from {1} at {2}", databaseName ?? "Default", selfInstance.Id, selfInstance.InternalUrl);

                    documentStore.Initialize();

                    using (var session = databaseName != null ? documentStore.OpenSession(databaseName) : documentStore.OpenSession())
                    {
                        var documentId = new ReplicationDocument().Id; // Just to stay in sync with changes from RavenDb

                        var replicationDocument = session.Load<ReplicationDocument>(documentId) ?? new ReplicationDocument();

                        replicationDocument.Destinations = instanceEnumerator.EnumerateInstances().Where(i => !i.IsSelf).Select(i => new ReplicationDestination() { Url = i.InternalUrl }).ToList();
                        session.Store(replicationDocument);
                        session.SaveChanges();
                    }
                }
            }
        }
    }
}
