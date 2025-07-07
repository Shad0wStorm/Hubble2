using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using ClientSupport.Utils;

namespace ClientSupport
{
    /// <summary>
    /// Implementation of the ProgressMonitor interface used to monitor
    /// progress for a specific project exposing the changes as property
    /// changed notifications so they can be bound on the UI thread.
    /// </summary>
    public class ProjectProgressMonitor : ProgressMonitor, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        private Project m_project;
        /// <summary>
        /// The Project being monitored.
        /// </summary>
        public Project Project { get { return m_project; } }

        public String Name
        {
            get { return m_project.Name; }
        }


        private ProgressWindow m_progressWindow;

        private String m_action;
        /// <summary>
        /// The action being performed currently.
        /// </summary>
        public String Action
        {
            get
            {
                return m_action;
            }
            set
            {
                if (value != m_action)
                {
                    m_action = value;
                    NotifyPropertyChanged("Action");
                }
            }
        }

        private Int64 m_total;
        /// <summary>
        /// The total used for establishing the progress indicator, or zero
        /// if the current action is not progress based.
        /// </summary>
        public Int64 Total
        {
            get
            {
                return m_total;
            }
            set
            {
                if (value != m_total)
                {
                    bool sp = ShowProgress;
                    m_total = value;
                    NotifyPropertyChanged("Total");
                    if (sp != ShowProgress)
                    {
                        NotifyPropertyChanged("ShowProgress");
                        NotifyPropertyChanged("ShowSecondaryProgress");
                    }
                }
            }
        }

        private Int64 m_current;
        /// <summary>
        /// The current progress through the task (0&lt;=Current&lt;=Total) or
        /// zero if the current task is not progress based.
        /// </summary>
        public Int64 Current
        {
            get
            {
                return m_current;
            }
            set
            {
                if (value != m_current)
                {
                    m_current = value;
                    NotifyPropertyChanged("Current");
                }
            }
        }

        private Int64 m_secondary;
        /// <summary>
        /// The current progress through the task (0&lt;=Current&lt;=Total) or
        /// zero if the current task is not progress based.
        /// </summary>
        public Int64 Secondary
        {
            get
            {
                return m_secondary;
            }
            set
            {
                if (value != m_secondary)
                {
                    m_secondary = value;
                    NotifyPropertyChanged("Secondary");
                    ShowSecondaryProgress = (m_total != 0) && (m_secondary != 0);
                }
            }
        }

        /// <summary>
        /// True if a progress indication can be shown, i.e. the current task
        /// is progress based, or false otherwise.
        /// </summary>
        public bool ShowProgress
        {
            get { return m_total != 0; }
        }

        private bool m_showSecondaryProgress = false;
        public bool ShowSecondaryProgress
        {
            get
            {
                return m_showSecondaryProgress;
            }
            set
            {
                if (m_showSecondaryProgress != value)
                {
                    m_showSecondaryProgress = value;
                    NotifyPropertyChanged("ShowSecondaryProgress");
                }
            }
        }

        private bool m_hasCompleted;
        /// <summary>
        /// True if the collection of tasks for this monitor has been
        /// completed.
        /// </summary>
        public bool HasCompleted
        {
            get
            {
                return m_hasCompleted;
            }
            set
            {
                if (value != m_hasCompleted)
                {
                    m_hasCompleted = value;
                    NotifyPropertyChanged("HasCompleted");
                }
            }
        }

        private bool m_canCancel;
        /// <summary>
        /// True if the current task can be cancelled using RequestCancellation.
        /// </summary>
        public bool CanCancel
        {
            get { return m_canCancel; }
            set
            {
                if (m_canCancel != value)
                {
                    m_canCancel = value;
                    NotifyPropertyChanged("CanCancel");
                }
            }
        }

        private String m_activeKey;
        private String m_secondaryKey;

        private bool m_cancellationRequested = false;
        /// <summary>
        /// Set to True to cancel the in progress task and skip any remaining
        /// tasks for the project.
        /// 
        /// The cancellation is handled by the task updating the monitor which
        /// may not respect the option, though in that case it should set
        /// CanCancel to false so that the UI can update accordingly rather
        /// than giving the user the option to cancel and then disregarding the
        /// request.
        /// </summary>
        public bool RequestCancellation
        {
            get { return m_cancellationRequested; }
            set
            {
                if (m_cancellationRequested != value)
                {
                    m_cancellationRequested = value;
                    NotifyPropertyChanged("RequestCancellation");
                }
            }
        }

        public ProjectProgressMonitor(Project p)
        {
            m_project = p;
            HasCompleted = false;
            m_progressWindow = new ProgressWindow(30);
        }

        public override void Start(String key)
        {
            HasCompleted = false;
        }

        public override void StartAction(String key, String action)
        {
            Action = action;
            Total = 10;
            Current = 0;
            Secondary = 0;
        }

        private DateTime m_progressStartTime;
        public override void StartProgressAction(String key, String action, Int64 target, bool canCancel)
        {
            if (m_activeKey == null)
            {
                m_activeKey = key;
                m_secondaryKey = key + "Secondary";
            }
            if (key == m_activeKey)
            {
                m_progressStartTime = DateTime.UtcNow;
                m_lastUpdateTime = m_progressStartTime;
                Action = action;
                Total = target;
                Current = 0;
                CanCancel = canCancel;
            }
            Secondary = 0;
        }

        private DateTime m_lastUpdateTime;

        private double m_totalSeconds;
        /// <summary>
        /// The total number of seconds the current ProgressAction has been 
        /// running for.
        /// </summary>
        public double TotalSeconds
        {
            get
            {
                return m_totalSeconds;
            }
            set
            {
                if (m_totalSeconds != value)
                {
                    m_totalSeconds = value;
                    NotifyPropertyChanged("TotalSeconds");
                }
            }
        }

        private double m_rate;
        /// <summary>
        /// The rate at which progress is being made, in the same units as
        /// Progress is being indicated.
        /// </summary>
        public double ProgressRate
        {
            get { return m_rate; }
            set
            {
                if (m_rate != value)
                {
                    m_rate = value;
                    NotifyPropertyChanged("ProgressRate");
                }
            }
        }
        private double m_rateMB;
        /// <summary>
        /// The rate at which progress is being made, in 1024*1024 of the same
        /// units being used for progress indication. In the typical case where
        /// progress is being measured as a number of bytes this corresponds
        /// to the rate in MB.
        /// </summary>
        public double ProgressRateMB
        {
            get { return m_rateMB; }
            set
            {
                if (m_rateMB != value)
                {
                    m_rateMB = value;
                    NotifyPropertyChanged("ProgressRateMB");
                }
            }
        }

        private String m_rateETE;
        public String ProgressETE
        {
            get
            {
                return m_rateETE;
            }
            set
            {
                if (m_rateETE != value)
                {
                    m_rateETE = value;
                    NotifyPropertyChanged("ProgressETE");
                }
            }
        }

        private bool m_showRate;
        /// <summary>
        /// True if the rate or derived values should be shown.
        /// </summary>
        public bool ShowRate
        {
            get { return m_showRate; }
            set
            {
                if (m_showRate != value)
                {
                    m_showRate = value;
                    NotifyPropertyChanged("ShowRate");
                }
            }
        }

        public override void ReportActionProgress(String key, Int64 progress)
        {
            if (Secondary != 0)
            {
                // Use the secondary for rate determination if available
                if (m_secondaryKey == key)
                {
                    UpdateProgressRateCalculation(Secondary, progress);
                }
            }
            else
            {
                UpdateProgressRateCalculation(Current, progress);
            }
            if (m_activeKey == key)
            {
                Current = progress;
            }
            else
            {
                if (m_secondaryKey==key)
                {
                    Secondary = progress;
                }
            }

            ShowRate = true;
        }

        private void UpdateProgressRateCalculation(Int64 previous, Int64 current)
        {
            DateTime updateTime = DateTime.UtcNow;
            TimeSpan diff = updateTime.Subtract(m_progressStartTime);
            TotalSeconds = diff.TotalSeconds;

            Int64 segmentSize = current - previous;
            diff = updateTime.Subtract(m_lastUpdateTime);

            double segmentTime = diff.TotalMilliseconds;
            UpdateProgressRate(segmentSize, segmentTime);
            m_lastUpdateTime = updateTime;
        }

        private void UpdateProgressRate(Int64 size, double time)
        {
            m_progressWindow.AddSample(size, time);
            ProgressRate = m_progressWindow.Rate;
            ProgressRateMB = m_progressWindow.RateMB;

            Int64 remaining = Total - Current;
            double rate = m_progressWindow.TotalSeconds / m_progressWindow.TotalQuantity;
            int ete = (int)(remaining * rate);
            TimeSpan span = new TimeSpan(0, 0, ete);
            String ETE = "";
            if (span.TotalHours>22)
            {
                ETE = ETE + String.Format("{0}d", span.Days+1);
            }
            else
            {
                if (span.TotalMinutes > 30)
                {
                    ETE = ETE + String.Format("{0}h", span.Hours+1);
                }
                else
                {
                    if (span.TotalSeconds > 30)
                    {
                        ETE = ETE + String.Format("{0}m", ((span.Minutes / 5) + 1) * 5);
                    }
                    else
                    {
                        ETE = "<1m";
                    }
                }
            }
            ProgressETE = ETE;
            //if (time > 0)
            //{
            //    ProgressRate = (1000 * size) / time;
            //    ProgressRateMB = (ProgressRate / (1024 * 1024));
            //}
        }

        public override bool CancellationRequested()
        {
            return m_cancellationRequested;
        }

        public override void CompleteAction(String key)
        {
            Action = null;
            Current = Total;
            ShowRate = false;
            CanCancel = false;
            Secondary = 0;
        }

        public override void Complete(String key)
        {
            Total = 0;
            Secondary = 0;
            HasCompleted = true;
        }

        public override void Fail(String key, String message)
        {
            Action = message;
            Total = 0;
            HasCompleted = true;
        }
    }
}
