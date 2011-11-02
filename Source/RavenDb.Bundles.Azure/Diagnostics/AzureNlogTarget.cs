using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog.Targets;

namespace RavenDb.Bundles.Azure.Diagnostics
{
    public class AzureNlogTarget : TargetWithLayout 
    {
        protected override void Write(NLog.LogEventInfo logEvent)
        {
            System.Diagnostics.Trace.WriteLine(logEvent.FormattedMessage);
        }
    }
}
