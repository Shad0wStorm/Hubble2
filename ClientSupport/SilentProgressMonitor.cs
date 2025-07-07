using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientSupport
{
    /// <summary>
    /// Dummy monitor that discards any progress information provided.
    /// </summary>
    class SilentProgressMonitor : ProgressMonitor
    {
        public override void Start(string key)
        {
        }

        public override void StartAction(string key, string action)
        {
        }

        public override void StartProgressAction(string key, string action, long target, bool canCancel = false)
        {
        }

        public override bool CancellationRequested()
        {
            return false;
        }

        public override void ReportActionProgress(string key, long progress)
        {
        }

        public override void CompleteAction(string key)
        {
        }

        public override void Complete(string key)
        {
        }

        public override void Fail(string key, string message)
        {
        }
    }
}
