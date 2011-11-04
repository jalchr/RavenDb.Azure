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
        public int          InstanceIndex   { get; set; }
        public string       FriendlyName    { get; set; }
        public string       ExternalUrl     { get; set; }
        public string       InternalUrl     { get; set; }
        public bool         IsSelf          { get; set; }

        public bool         IsRoleMaster
        {
            get { return InstanceIndex == 0; }
        }
    }

    public interface IInstanceEnumerator
    {
        IEnumerable<InstanceDescription> EnumerateInstances();
    }
}