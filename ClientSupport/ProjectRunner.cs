using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using LocalResources;

namespace ClientSupport
{
    /// <summary>
    /// Class used to execute a project instance.
    /// </summary>
    public class ProjectRunner
    {
        /// <summary>
        /// Event and associated arguments class/delegate.
        /// 
        /// The event is raised when the project execution completes.
        /// </summary>
        public class ProjectCompletedEventArgs : EventArgs
        {
            public Project project;
            public ProjectCompletedEventArgs(Project p)
            {
                project = p;
            }
        }

        public delegate void ProjectCompletedEventHandler(object sender, ProjectCompletedEventArgs e);

        public event ProjectCompletedEventHandler ProjectCompleted;

        // The Project being executed.
        private Project m_project;

        /// <summary>
        /// Path to the watchdog executable that monitors the project for
        /// crashes and sends a report to the server if necessary.
        /// </summary>
        private String m_watchDog;

        /// <summary>
        /// Options to be interpreted by watchdog.
        /// </summary>
        private String m_watchDogOptions;

        /// <summary>
        /// Options to be interpreted by the application run by watchdog.
        /// </summary>
        private String m_targetOptions;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="run">The project to execute.</param>
        /// <param name="watchDog">Path to the watchdog executable.</param>
        /// <param name="watchDogOptions">Options to pass directly on the command line</param>
        /// <param name="targetOptions">
        ///   Options to pass to the final executable. If there is not watchdog
        ///   this will be no different from watchDogOptions, but if a watchdog
        ///   of some kind is being used then the target options should be
        ///   passed to the target launched by the watchdog, not handled by
        ///   watchdog.
        /// </param>
        public ProjectRunner(Project run, String watchDog, String watchDogOptions, String targetOptions)
        {
            m_project = run;
            m_watchDog = watchDog;
            m_watchDogOptions = watchDogOptions;
            m_targetOptions = targetOptions;
        }

        /// <summary>
        /// The arguments passed via command line, may themselves be a cmdline collection of args.
        /// for Example --> /appArg0 Fred /appArg1 Dave /subAppArgs "/appArg0 "Mike Jones" /apArg1 Sally"
        /// In this case, we need to double quote any single quotes as args such as Mike Jones will be
        /// broken up other wise.
        /// </returns>
        string FormatSubArgs(string _arg)
        {
            return _arg.Replace("\"", "\"\"");
        }

        /// <summary>
        /// Start execution of the project passed to the constructor.
        /// 
        /// If available the watchdog is used so that crashes in the project
        /// can be caught and reported.
        /// 
        /// The passed token is given to the executable/watchdog so they can
        /// use the existing authorisation to communicate with the server if
        /// required.
        /// 
        /// Register an event handler so we get notified when the started
        /// application completes.
        /// 
        /// This process is complicated by the way the game handles command
        /// line arguments, specifically there is a "ServerToken" argument
        /// which is bundled in quotes with the options that affect it, which
        /// the game then sees as a single option and breaks down internally.
        /// Other options such as the language are sent as entirely separate 
        /// arguments. For now assume the additional arguments passed into this
        /// function are server related and add them to the quoted set of
        /// options rather than being independent.
        /// </summary>
        /// <param name="token">Authentication token.</param>
        /// <returns>
        /// Null on success, otherwise a text message describing the problem
        /// encountered.
        /// </returns>
        public String Run(UserDetails userDetails, string machineId, string serverTime, String additionalArgs)
        {
            String executablePath = m_project.ExecutablePath;
            if (executablePath == null)
            {
                return LocalResources.Properties.Resources.ProjectRunner_NoStartup;

            }
            if (!File.Exists(executablePath))
            {
                return LocalResources.Properties.Resources.ProjectRunner_MissingStartup;
            }

            var projectArgs = FormatSubArgs(m_project.Arguments ?? "");

            // Handle being given some random other arguments from an unknown
            // source, i.e. not one we know about here, and not one that has
            // arrived from the project information.
            // Make sure we format it just in case.
            String extraArgs = "";
            /*
            if (!String.IsNullOrEmpty(additionalArgs))
            {
                extraArgs = " " + FormatSubArgs(additionalArgs);
            }
             */
            if (!String.IsNullOrEmpty(m_targetOptions))
            {
                extraArgs += " " + FormatSubArgs(m_targetOptions);
            }

            String serverArgs = additionalArgs;
            if (!String.IsNullOrEmpty(serverArgs))
            {
                int gao = serverArgs.IndexOf(" | ");
                if (gao >= 0)
                {
                    String gameArgs = serverArgs.Substring(gao + 3);
                    if (!String.IsNullOrEmpty(gameArgs))
                    {
                        extraArgs += " " + gameArgs;
                    }
                    serverArgs = serverArgs.Substring(0, gao);
                }
            }

            if (!m_project.Offline)
            {
                String authArgs = String.Format(" \"\"ServerToken {0} {1} {2}\"\"",
                    userDetails.AuthenticationToken, userDetails.SessionToken, serverArgs);
                projectArgs += authArgs;
            }
            projectArgs += extraArgs;

            ProcessStartInfo pstart = new ProcessStartInfo();
            String arguments = "";
            if ((m_watchDog != null) && (!m_project.Offline))
            {
                pstart.FileName = m_watchDog;
                arguments = string.Format("/Executable \"{0}\" /ExecutableArgs \" {1}\" /MachineToken {2} /Version {3} /AuthToken {4} /MachineId {5} /Time {6}",
                    executablePath, projectArgs, userDetails.AuthenticationToken, m_project.Version, userDetails.SessionToken, machineId, serverTime);
                if (!String.IsNullOrEmpty(m_project.ExecutableHash))
                {
                    arguments += string.Format(" /ExecutableHash \"{0}\"", m_project.ExecutableHash.ToUpperInvariant());
                }

                if (!String.IsNullOrEmpty(m_watchDogOptions))
                {
                    arguments += " " + FormatSubArgs(m_watchDogOptions);
                }
            }
            else
            {
                pstart.FileName = executablePath;
                arguments = projectArgs;
            }

            pstart.WorkingDirectory = GetProjectRunDirectory(executablePath);
            pstart.Arguments = arguments;

#if !DEBUG
            // Prevent the standard CMD window opening on the created
            // application.
            pstart.CreateNoWindow = true;
            pstart.UseShellExecute = false;
#endif

            m_project.Action = Project.ActionType.Disabled;
            try
            {
                Process pid = Process.Start(pstart);
                pid.EnableRaisingEvents = true;
                pid.Exited += ExecutionFinished;
            }
            catch (System.Exception ex)
            {
                return String.Format(LocalResources.Properties.Resources.ProjectRunner_FailedStartup,
                    m_project.Name, ex.Message);
            }
            return null;
        }

        /// <summary>
        /// Fiddle the working directory in case the product uses a
        /// subdirectory to hold the runtime files.
        /// 
        /// Directory will only be modified from the project directory if the
        /// new directory is contained within the project directory as some
        /// crude form of sandboxing.
        /// </summary>
        /// <param name="executablePath">Path to the executable to run.</param>
        /// <returns>Working directory to use.</returns>
        private String GetProjectRunDirectory(String executablePath)
        {
            String twd = m_project.ProjectDirectory;
            String parent = Path.GetDirectoryName(executablePath);
            String test = parent;
            while (test != null)
            {
                if (m_project.ProjectDirectory == test)
                {
                    test = null;
                    twd = parent;
                }
                else
                {
                    String newtest = Path.GetDirectoryName(test);
                    if (newtest == test)
                    {
                        test = null;
                    }
                    else
                    {
                        test = newtest;
                    }
                }
            }
            return twd;
        }

        /// <summary>
        /// Called by the system when the started application has exited.
        /// 
        /// Update the project to reflect the current state, and if anyone has
        /// registered to receive our completion event, signal it.
        /// </summary>
        /// <param name="sender">The sender of the event(unused)</param>
        /// <param name="e">Event prameters (unused)</param>
        private void ExecutionFinished(object sender, EventArgs e)
        {
            m_project.Update();
            ProjectCompletedEventHandler handler = ProjectCompleted;
            if (handler != null)
            {
                handler(this, new ProjectCompletedEventArgs(m_project));
            }
        }
    }
}
