using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace ManifestTool
{
    class ManifestTidyWorker : WorkerBase
    {
        public FileStore ActiveFileStore { get; set; }
        public String Report { get; set; }

        public ManifestTidyWorker()
        {
            m_progressWindow.Title = "Tidy File Store";
        }

        public override void Run()
        {
            m_progressWindow.Information = "Tidying";
            m_progressWindow.Action = "Scanning manifests";
            base.Run();
        }

        public override void ExecuteTask(DoWorkEventArgs e)
        {
            String summary = ActiveFileStore.Tidy(ProgressReport);
            if (summary!=null)
            {
                Report = "Tidy Successful\r\n"+summary;
            }
            else
            {
                Report = "Tidy failed";
            }
        }

        public void ProgressReport(String action, int progress, int total)
        {
            m_progressWindow.Action = action;
            m_worker.ReportProgress((100*progress)/total);
        }
    }
}
