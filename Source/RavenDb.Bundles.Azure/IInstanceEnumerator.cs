using System.Collections.Generic;
using System.Linq;
using Raven.Abstractions.Replication;

namespace RavenDb.Bundles.Azure
{
    public enum InstanceType
    {
        Read,
        ReadWrite,
    }

    public class InstanceDescription
    {
        public string       Id              { get; set; }
        public InstanceType InstanceType    { get; set; }
        public string       ExternalUrl     { get; set; }
        public string       InternalUrl     { get; set; }
        public bool         IsSelf          { get; set; }
    }

    public interface IInstanceEnumerator
    {
        IEnumerable<InstanceDescription> EnumerateInstances();
    }
}