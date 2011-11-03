using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace RavenDb.Bundles.Azure
{
    public static class RoleInstanceExtensions
    {
        public static string GetFriendlyName( this RoleInstance instance )
        {
            return instance.Id.Replace("-", string.Empty).Replace("_", string.Empty).Replace(".", string.Empty).Replace("(", string.Empty).Replace(")", String.Empty).ToLowerInvariant();
        }
    }
}
