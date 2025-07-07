using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ClientSupport.ProjectUpdater
{
    public interface Command
    {
        bool Execute();
    }

    public interface CommandQueueProgress
    {
        void ReportUpdate();
    }

    public class CommandQueue : CommandQueueProgress
    {
        Queue<Command> m_commands;
        List<Command> m_running;
        private Mutex m_mutex;

        public delegate void CommandCompletionHandler(object sender);
        public event CommandCompletionHandler CommandCompletionEvent;

        public bool IsEmpty
        {
            get
            {
                m_mutex.WaitOne();
                bool empty = (m_commands.Count == 0);
                m_mutex.ReleaseMutex();
                return empty;
            }
        }
        public bool IsComplete
        {
            get
            {
                m_mutex.WaitOne();
                bool complete = (m_commands.Count == 0) && (m_running.Count == 0);
                m_mutex.ReleaseMutex();
                return complete;
            }
        }

        public CommandQueue()
        {
            m_commands = new Queue<Command>();
            m_running = new List<Command>();
            m_mutex = new Mutex();
        }

        public void AddCommand(Command command)
        {
            m_mutex.WaitOne();
            m_commands.Enqueue(command);
            m_mutex.ReleaseMutex();
        }

        public Command NextCommand()
        {
            Command command = null;
            m_mutex.WaitOne();
            if (m_commands.Count > 0)
            {
                command = m_commands.Dequeue();
                m_running.Add(command);
            }
            else
            {
                command = null;
            }
            m_mutex.ReleaseMutex();
            return command;
        }

        public void Complete(Command command)
        {
            m_mutex.WaitOne();
            m_running.Remove(command);
            m_mutex.ReleaseMutex();
            ReportUpdate();
        }

        public void ReportUpdate()
        {
            if (CommandCompletionEvent != null)
            {
                CommandCompletionEvent(this);
            }
        }

        public void Process(int threadCount)
        {
            int tc = threadCount == 0 ? Environment.ProcessorCount : threadCount;
            Thread[] threads = new Thread[tc];

            for (int t=0; t<tc; ++t)
            {
                threads[t] = new Thread(RunThread);
                threads[t].CurrentCulture = Thread.CurrentThread.CurrentCulture;
                threads[t].CurrentUICulture = Thread.CurrentThread.CurrentUICulture;
                threads[t].Start(this);
            }

            for (int t = 0; t < tc; ++t)
            {
                threads[t].Join();
            }
        }

        private static void RunThread(object cq)
        {
            CommandQueue workSource = cq as CommandQueue;
            if (workSource!=null)
            {
                bool running = true;
                while ((!workSource.IsComplete) && running)
                {
                    Command next = workSource.NextCommand();
                    if (next != null)
                    {
                        running = next.Execute();
                        workSource.Complete(next);
                    }
                    else
                    {
                        Thread.Sleep(50);
                    }
                }
            }
        }
    }
}
