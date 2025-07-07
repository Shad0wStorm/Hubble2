using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using ClientSupport;

namespace ManifestTool
{
    class ManifestValidateWorker : WorkerBase
    {
        public ManifestFile Source { get; set; }
        public FileStore ActiveFileStore { get; set; }
        public String Report { get; set; }

        public ManifestValidateWorker()
        {
            m_progressWindow.Title = "Validate Manifest";
        }

        public override void Run()
        {
            String info = "Validating ";
            if (Source.ProductTitle != null)
            {
                info += Source.ProductTitle;
            }
            else
            {
                info += Source.FileName;
            }
            if (Source.ProductVersion != null)
            {
                info += " (" + Source.ProductVersion + ")";
            }
            m_progressWindow.Information = info;
            m_progressWindow.Action = "Initialising";
            base.Run();
        }

        public override void ExecuteTask(DoWorkEventArgs e)
        {
            int progress = 0;
            int total = Source.EntryCount;
            m_action = "Validating";
            m_worker.ReportProgress(0);

            int counted = 0;
            Report = "";

            foreach (ManifestFile.ManifestEntry entry in Source.Entries)
            {
                if (!ActiveFileStore.Contains(entry.Hash))
                {
                    if (counted < 15)
                    {
                        Report = Report + "File '" + entry.Path + "' is missing.\n";
                    }
                    ++counted;
                }
                ++progress;
                m_worker.ReportProgress((progress*100)/total);
            }

            if (String.IsNullOrEmpty(Report))
            {
                Report = "Validation Successful.";
            }
            else
            {
                Report += "Total " + counted.ToString() + " files missing.";
            }
        }
    }
}
