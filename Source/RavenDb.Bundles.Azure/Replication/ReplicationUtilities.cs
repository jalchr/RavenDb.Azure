using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using Raven.Abstractions.Data;
using Raven.Abstractions.Replication;
using Raven.Client.Document;
using Raven.Database;
using Raven.Json.Linq;

namespace RavenDb.Bundles.Azure.Replication
{
    public static class ReplicationUtilities
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        public static void UpdateReplication( InstanceDescription selfInstance,IInstanceEnumerator instanceEnumerator,DocumentDatabase database )
        {
            log.Info("Ensuring default database is replicated from {0} at {1}", selfInstance.Id, selfInstance.InternalUrl);

            var documentId = new ReplicationDocument().Id;

            var replicationDocument = new ReplicationDocument()
            {
                Destinations =
                    EnumerateReplicationDestinations(instanceEnumerator,selfInstance.IsRoleMaster).
                    Select(i => new ReplicationDestination() { Url = i.InternalUrl }).
                    ToList()
            };

            database.Put(documentId, null, RavenJObject.FromObject(replicationDocument), new RavenJObject(), null);
        }

        public static void UpdateReplication( InstanceDescription selfInstance,IInstanceEnumerator instanceEnumerator,string databaseName )
        {
            // Setup replication:
            using (var documentStore = new DocumentStore() { Url = selfInstance.InternalUrl })
            {
                log.Info("Ensuring database {0} is replicated from {1} at {2}", databaseName, selfInstance.Id, selfInstance.InternalUrl);

                documentStore.Initialize();

                using (var session = documentStore.OpenSession(databaseName) )
                {
                    var documentId = new ReplicationDocument().Id; // Just to stay in sync with changes from RavenDb

                    var replicationDocument = session.Load<ReplicationDocument>(documentId) ?? new ReplicationDocument();

                    replicationDocument.Destinations = EnumerateReplicationDestinations(instanceEnumerator,selfInstance.IsRoleMaster).Select(i => new ReplicationDestination() { Url = string.Format("{0}/databases/{1}",i.InternalUrl,databaseName)}).ToList();
                    session.Store(replicationDocument);
                    session.SaveChanges();
                }
            }
        }

        private static IEnumerable<InstanceDescription> EnumerateReplicationDestinations(IInstanceEnumerator instanceEnumerator,bool isRoleMaster)
        {
            if (isRoleMaster)
            {
                return instanceEnumerator.EnumerateInstances().Where(i => !i.IsSelf);
            }

            return
                instanceEnumerator.EnumerateInstances().Where(
                    i => !i.IsSelf && i.InstanceType == InstanceType.ReadWrite && i.IsRoleMaster);
        }
    }
}
