using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;
using NLog;
using NLog.Config;
using NLog.Layouts;
using RavenDb.Bundles.Azure.Diagnostics;
using LogLevel = Microsoft.WindowsAzure.Diagnostics.LogLevel;

namespace RavenDb.Bundles.Azure
{
    public static class AzureIntegration
    {
        public const string DiagnosticsConnectionStringKey  = "Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString";

        public const string StorageConnectionStringKey      = "RavenDb.Storage";
        public const string StorageCacheKey                 = "RavenDb.Storage.Cache";
        public const string StorageContainerName            = "drives";

        public const string LogLevelKey                     = "LogLevel";
        public const string LogTransferPeriodKey            = "LogLevelTransferPeriod";
        public const string LogLayout                       = "LogLayout";
        public const string EventLogNamesKey                = "EventLogsToTransfer";

        private static readonly Logger      log                 = LogManager.GetCurrentClassLogger();
        private static readonly object      initializationLock  = new object();
        private static bool                 isInitialized       = false;

        private static CloudStorageAccount  cloudStorageAccount;
        private static CloudBlobClient      cloudBlobClient;
        private static CloudBlobContainer   cloudBlobContainer;
        private static CloudDrive           cloudDrive;
        private static string               storagePath;

        private static LocalResource        localCacheResource;

        public static string GetStoragePathForDatabase( RoleInstance instance,string databaseName )
        {
            EnsureInitialization();

            var subDirectoryPath    = string.IsNullOrWhiteSpace(databaseName) ? "Data" : Path.Combine("Tenants", databaseName);
            var path                = Path.Combine(storagePath, subDirectoryPath);

            log.Info("Storage path for database {0} is {1}",databaseName ?? "Default",path);

            if (!Directory.Exists(path))
            {
                log.Info("Created storage directory for database {0} at {1}",databaseName ?? "Default",path);
                Directory.CreateDirectory(path);
            }

            return path;
        }

        private static void EnsureInitialization()
        {
            if (!isInitialized)
            {
                lock (initializationLock)
                {
                    if (!isInitialized)
                    {
                        Initialize();
                        isInitialized = true;
                    }
                }
            }
        }

        private static void Initialize()
        {
            try
            {
                InitializeDiagnostics();

                RoleEnvironment.Stopping += (sender, stopArguments) => Shutdown();

                CloudStorageAccount.SetConfigurationSettingPublisher((key, callback) => { });

                cloudStorageAccount = CloudStorageAccount.FromConfigurationSetting(StorageConnectionStringKey) ??
                                      CloudStorageAccount.DevelopmentStorageAccount;

                localCacheResource = RoleEnvironment.GetLocalResource(StorageCacheKey);

                log.Info("Initializing cloud drive cache, size: {0} mb", localCacheResource.MaximumSizeInMegabytes);
                CloudDrive.InitializeCache(localCacheResource.RootPath, localCacheResource.MaximumSizeInMegabytes);

                cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
                cloudBlobContainer = cloudBlobClient.GetContainerReference(StorageContainerName);
                log.Info("Cloud blob container selected: {0}", cloudBlobContainer.Uri);

                cloudBlobContainer.CreateIfNotExist();
                log.Info("Cloud blob existence check complete");

                var driveName = RoleEnvironment.CurrentRoleInstance.GetFriendlyName() + ".vhd";
                log.Info("Cloud drive name: {0}", driveName);

                var pageBlob = cloudBlobContainer.GetPageBlobReference(driveName);

                cloudDrive = cloudStorageAccount.CreateCloudDrive(pageBlob.Uri.ToString());
                cloudDrive.CreateIfNotExist(localCacheResource.MaximumSizeInMegabytes);

                log.Info("Cloud drive created");

                storagePath = cloudDrive.Mount(localCacheResource.MaximumSizeInMegabytes, DriveMountOptions.None);

                log.Info("Cloud drive virtual storage path: {0}", storagePath);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Azure integration initialization failure");
                Trace.WriteLine(ex);

                var storageException = FindExceptionInChain<StorageException>(ex);

                if (storageException != null)
                {
                    Trace.WriteLine(StorageExceptionToString(storageException));
                }

                RoleEnvironment.RequestRecycle();
            }
        }

        private static void Shutdown()
        {
            log.Info("Shutdown called");
        
            cloudDrive.Unmount();

            log.Info("Cloud drive unmounted");
        }

        private static void InitializeDiagnostics()
        {
            // Add azure trace listener:
            var traceListener = new DiagnosticMonitorTraceListener();
            Trace.Listeners.Add(traceListener);

            // Configure diagnostics monitor:
            var dmc = DiagnosticMonitor.GetDefaultInitialConfiguration();

            // Log level is defined by nlog:
            dmc.Logs.ScheduledTransferLogLevelFilter = LogLevel.Verbose;

            // Configure log transfer period:
            var transferPeriod = TimeSpan.FromMinutes(GetConfigurationValue(LogTransferPeriodKey,1.0));
            dmc.Logs.ScheduledTransferPeriod = transferPeriod;
            dmc.WindowsEventLog.ScheduledTransferPeriod = transferPeriod;

            // Add any event logs we want to see:
            foreach( var eventLogName in GetConfigurationValue(EventLogNamesKey, "Application!*;System!*").Split(';') )
            {
                dmc.WindowsEventLog.DataSources.Add(eventLogName);
            }

            // Initialize the monitor:
            DiagnosticMonitor.Start(DiagnosticsConnectionStringKey, dmc);

            // Create nlog configuration:
            var loggingConfiguration = new LoggingConfiguration();
            var azureTarget = new AzureNlogTarget { Layout = new SimpleLayout(GetConfigurationValue(LogLayout, "${date:format=HH\\:MM\\:ss} ${logger} ${message}")) };
            loggingConfiguration.AddTarget("azure",azureTarget);

            var logLevelName    = GetConfigurationValue(LogLevelKey, "Debug");
            var logLevel        = (NLog.LogLevel)typeof(NLog.LogLevel).GetField(logLevelName, BindingFlags.Public | BindingFlags.Static).GetValue(null);

            loggingConfiguration.LoggingRules.Add(new LoggingRule("*",logLevel,azureTarget));
            LogManager.Configuration = loggingConfiguration;
        }

        private static TValue GetConfigurationValue<TValue>( string key,TValue defaultValue )
        {
            try
            {
                var rawValue = RoleEnvironment.GetConfigurationSettingValue(key);
                return (TValue)Convert.ChangeType(rawValue, typeof (TValue));
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        private static TException FindExceptionInChain<TException>( Exception ex )
            where TException : Exception
        {
            while (ex != null)
            {
                if (ex is TException)
                {
                    return (TException) ex;
                }

                ex = ex.InnerException;
            }

            return null;
        }

        private static string StorageExceptionToString( StorageException ex )
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("Storage error: {0}", ex.ErrorCode);
            stringBuilder.AppendLine();

            if (ex.ExtendedErrorInformation != null)
            {
                stringBuilder.AppendFormat("Error message: {0}", ex.ExtendedErrorInformation.ErrorMessage);
                stringBuilder.AppendLine();

                if (ex.ExtendedErrorInformation.AdditionalDetails != null)
                {
                    foreach (var key in ex.ExtendedErrorInformation.AdditionalDetails)
                    {
                        var value = ex.ExtendedErrorInformation.AdditionalDetails[(string)key];
                        stringBuilder.AppendFormat("{0} = {1}", key, value);
                        stringBuilder.AppendLine();
                    }
                }
            }

            return stringBuilder.ToString();
        }
    }
}
