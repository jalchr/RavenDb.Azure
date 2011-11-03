using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RavenDb.Bundles.Azure.Configuration
{
    public static class ConfigurationSettingsKeys
    {
        public const string DiagnosticsConnectionString         = "Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString";
        public const string DiagnosticsTransferPeriodInMinutes  = "Diagnostics.TransferPeriodInMinutes";
        public const string DiagnosticsEventLogsToTransfer      = "Diagnostics.EventLogsToTransfer";
        public const string DiagnosticsLogLayout                = "Diagnostics.LogLayout";
        public const string DiagnosticsLogLevel                 = "Diagnostics.LogLevel";

        public const string StorageConnectionString             = "Storage.ConnectionString";
        public const string StorageContainerName                = "Storage.ContainerName";
        public const string StorageSize                         = "Storage.Size";
        public const string StorageCacheResource                = "Storage.Cache";
    }
}
