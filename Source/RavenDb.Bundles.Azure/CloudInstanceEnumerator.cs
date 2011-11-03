using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace RavenDb.Bundles.Azure
{
    [Export(typeof(IInstanceEnumerator))]
    public class CloudInstanceEnumerator : IInstanceEnumerator
    {
        public IEnumerable<InstanceDescription> EnumerateInstances()
        {
            var instances = RoleEnvironment.Roles.SelectMany(r => r.Value.Instances);

            return instances.Select(i => new InstanceDescription()
            {
                Id = i.Id,
                ExternalUrl = EndpointToUrl(i.InstanceEndpoints["PublicHttpEndpoint"]),
                InternalUrl = EndpointToUrl(i.InstanceEndpoints["PrivateHttpEndpoint"]),
                InstanceType = i.Role.Name.IndexOf("Write", StringComparison.OrdinalIgnoreCase) >= 0 ? InstanceType.ReadWrite : InstanceType.Read,
                IsSelf = i.Id.Equals(RoleEnvironment.CurrentRoleInstance.Id, StringComparison.OrdinalIgnoreCase)
            });
        }

        private static string EndpointToUrl( RoleInstanceEndpoint endpoint )
        {
            return string.Format("{0}://{1}:{2}", endpoint.Protocol, endpoint.IPEndpoint.Address, endpoint.IPEndpoint.Port);
        }
    }
}
