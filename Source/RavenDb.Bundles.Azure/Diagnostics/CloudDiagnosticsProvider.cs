using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.WindowsAzure.Diagnostics;
using NLog;
using NLog.Config;
using NLog.Layouts;
using RavenDb.Bundles.Azure.Configuration;
using LogLevel = Microsoft.WindowsAzure.Diagnostics.LogLevel;

namespace RavenDb.Bundles.Azure.Diagnostics
{
    [Export(typeof(IDiagnosticsProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class CloudDiagnosticsProvider : IDiagnosticsProvider
    {
        private readonly object initializationLock  = new object();
        private bool            isInitialized       = false;

        [Import]
        public IConfigurationProvider ConfigurationProvider { get; set; }

        public void Initialize()
        {
            if (!isInitialized)
            {
                lock (initializationLock)
                {
                    if (!isInitialized)
                    {
                        OnInitialize();
                        isInitialized = true;
                    }
                }
            }
        }

        private void OnInitialize()
        {
            // Add azure trace listener:
            var traceListener = new DiagnosticMonitorTraceListener();
            Trace.Listeners.Add(traceListener);

            // Configure diagnostics monitor:
            var dmc = DiagnosticMonitor.GetDefaultInitialConfiguration();

            // Log level is defined by nlog:
            dmc.Logs.ScheduledTransferLogLevelFilter    = LogLevel.Verbose;

            // Configure log transfer period:
            var transferPeriod                          = TimeSpan.FromMinutes(ConfigurationProvider.GetSetting(ConfigurationSettingsKeys.DiagnosticsTransferPeriodInMinutes,0.3));

            dmc.Logs.ScheduledTransferPeriod            = transferPeriod;
            dmc.WindowsEventLog.ScheduledTransferPeriod = transferPeriod;

            // Add any event logs we want to see:
            foreach (var eventLogName in ConfigurationProvider.GetSetting(ConfigurationSettingsKeys.DiagnosticsEventLogsToTransfer, "Application!*;System!*").Split(';'))
            {
                dmc.WindowsEventLog.DataSources.Add(eventLogName);
            }

            // Initialize the monitor:
            DiagnosticMonitor.Start(ConfigurationSettingsKeys.DiagnosticsConnectionString, dmc);

            // Create nlog configuration:
            var loggingConfiguration    = new LoggingConfiguration();
            var logLayout               = ConfigurationProvider.GetSetting(ConfigurationSettingsKeys.DiagnosticsLogLayout, "${date:format=HH\\:MM\\:ss} ${logger} ${message} ${exception}");

            var traceTarget = new NLog.Targets.TraceTarget() {Layout = logLayout};
            loggingConfiguration.AddTarget("trace", traceTarget);

            var logLevelName = ConfigurationProvider.GetSetting(ConfigurationSettingsKeys.DiagnosticsLogLevel, "Debug");
            var logLevel = (NLog.LogLevel)typeof(NLog.LogLevel).GetField(logLevelName, BindingFlags.Public | BindingFlags.Static).GetValue(null);

            loggingConfiguration.LoggingRules.Add(new LoggingRule("*", logLevel, traceTarget));
            LogManager.Configuration = loggingConfiguration;
        }
    }
}
