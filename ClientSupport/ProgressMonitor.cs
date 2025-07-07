using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientSupport
{
    /// <summary>
    /// Interface for monitoring tasks and subtasks.
    /// 
    /// The interface is abstract and does not imply how the information should
    /// be presented to the user, it simply provides a mechanism by which
    /// progress can be reported.
    /// 
    /// The interface will be called on the thread doing the reporting so if
    /// this is not suitable, for example a background thread is reporting on
    /// work performed that needs to be used to update the UI, then it is
    /// the responsibility of the implementor of the monitor ensure the update
    /// occurs on the correct thread using Invoke for example.
    /// </summary>
    public abstract class ProgressMonitor
    {
        /// <summary>
        /// Start a series of actions to be monitored. There must be a
        /// corresponding call to either Complete OR Fail.
        /// </summary>
        /// <param name="key">
        /// Key used to monitor a set of sub tasks allowing a single monitor
        /// to track multiple parallel tasks if required.
        /// </param>
        public abstract void Start(String key);

        /// <summary>
        /// Start a specific action (sub task) within a task. Actions are
        /// performed sequentially so there must be a corresponding
        /// CompleteAction before starting a new action.
        /// </summary>
        /// <param name="key">Key for the task as passed to Start.</param>
        /// <param name="action">Name of the current action within the task.</param>
        public abstract void StartAction(String key, String action);

        /// <summary>
        /// Start a specific action within a task where the sub task is likely
        /// to take a significant time, and the subtask can provide a useful
        /// indication of progress within the task.
        /// </summary>
        /// <param name="key">Key for the task as passed to Start.</param>
        /// <param name="action">Name of the current action within the task.</param>
        /// <param name="target">A number indicating completion.</param>
        /// <param name="canCancel">True if the action supports cancellation.</param>
        public abstract void StartProgressAction(String key, String action, Int64 target, bool canCancel = false);

        /// <summary>
        /// Test whether the UI has requested the current progress action to
        /// be cancelled.
        /// </summary>
        /// <returns>True if cancellation has been requested.</returns>
        public abstract bool CancellationRequested();

        /// <summary>
        /// Report progress on an action previously started with
        /// StartProgressAction.
        /// </summary>
        /// <param name="key">
        /// Key previously passed to Start and subsequently
        /// StartProgressAction. It is not necessary to pass the action name
        /// since only one action (for the given key) can be in progress at
        /// any time.
        /// </param>
        /// <param name="progress">
        /// A value indicating progress through the process it should be
        /// 0&lt;=progress&lt;=target where target is the value passed to 
        /// StartProgressAction.
        /// </param>
        public abstract void ReportActionProgress(String key, Int64 progress);

        /// <summary>
        /// Complete an action previously started using StartAction or
        /// StartProgressAction. Where the action was started with
        /// StartProgressAction it is not necessary for a call to have been
        /// made to ReportActionProgress with progress=target
        /// </summary>
        /// <param name="key">
        /// The key previously passed to Start and StartAction or
        /// StartProgressAction.
        /// </param>
        public abstract void CompleteAction(String key);

        /// <summary>
        /// The task has completed successfully. The monitor may, but is not
        /// required to, delete any indication of the task being in progress.
        /// For example it may show a finished indication for a short time
        /// before removing the task completely.
        /// 
        /// Once Complete or Fail have been called it is safe to start a new
        /// task using the same key. It is up to the monitor whether to
        /// represent that as a new task or reuse the indication for the
        /// existing task with the same name.
        /// </summary>
        /// <param name="key">The key previously passed to Start.</param>
        public abstract void Complete(String key);

        /// <summary>
        /// The task has completed unsuccessfully. The monitor should treat
        /// this as a call to Complete, but it should also display the
        /// associated error message in some way to alert the user of the
        /// failure.
        /// </summary>
        /// <param name="key">The key previously passed to Start.</param>
        /// <param name="message">
        /// A message indicating the reason for failure.
        /// </param>
        public abstract void Fail(String key, String message);
    }
}
