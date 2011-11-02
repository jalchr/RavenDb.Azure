using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace RavenDb.Bundles.Azure
{
    public static class RoleEnvironmentUtilities
    {
        public const string InternalEndpointKey = "PrivateHttpEndpoint";

        public static IEnumerable<string> GetAllInternalEndpointsExceptThis()
        {
            return GetAllInstancesExceptThis().Select(i =>
            {
                var endpoint = i.InstanceEndpoints[InternalEndpointKey];
                return EndpointToUrl(endpoint);
            });
        }

        private static string EndpointToUrl( RoleInstanceEndpoint endpoint )
        {
            return string.Format("{0}://{1}:{2}", endpoint.Protocol, endpoint.IPEndpoint.Address,
                                 endpoint.IPEndpoint.Port);
        }

        private static IEnumerable<RoleInstance> GetAllInstancesExceptThis()
        {
            return
                GetAllInstances().Where(
                    i => !i.Id.Equals(RoleEnvironment.CurrentRoleInstance.Id, StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<RoleInstance> GetAllInstances()
        {
            return RoleEnvironment.Roles.Where(r => r.Value != null).SelectMany(r => r.Value.Instances);
        }
    }
}
