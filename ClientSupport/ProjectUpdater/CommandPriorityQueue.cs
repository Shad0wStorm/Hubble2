using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace ClientSupport.ProjectUpdater
{
    public interface PriorityCommand : Command
    {
        int Priority();
    }

    public class CommandPriorityQueue : CommandQueueProgress
    {
        public const int Highest = 0;
        public const int High = 1;
        public const int Normal = 2;
        public const int Low = 3;
        public const int Lowest = 4;
        public const int Limit = 5;

        Queue<PriorityCommand>[] m_commands;
        List<PriorityCommand> m_running;
        private Mutex m_mutex;

        public delegate void CommandCompletionHandler(object sender);
        public event CommandCompletionHandler CommandCompletionEvent;

        private int QueuedCommands()
        {
            int total = 0;
            for (int i = 0; i < m_commands.Length; ++i)
            {
                total += m_commands[i].Count;
            }
            return total;
        }

        public bool IsEmpty
        {
            get
            {
                m_mutex.WaitOne();
                bool empty = QueuedCommands()==0;
                m_mutex.ReleaseMutex();
                return empty;
            }
        }
        public bool IsComplete
        {
            get
            {
                m_mutex.WaitOne();
                bool complete = (QueuedCommands() == 0) && (m_running.Count == 0);
                m_mutex.ReleaseMutex();
                return complete;
            }
        }

        public CommandPriorityQueue()
        {
            m_commands = new Queue<PriorityCommand>[Limit];
            for (int i = 0; i < Limit; ++i)
            {
                m_commands[i] = new Queue<PriorityCommand>();
            }
            m_running = new List<PriorityCommand>();
            m_mutex = new Mutex();
        }

        public void AddCommand(PriorityCommand command)
        {
            m_mutex.WaitOne();
            int priority = command.Priority();
            Debug.Assert(priority < Limit);
            m_commands[priority].Enqueue(command);
            m_mutex.ReleaseMutex();
        }

        public PriorityCommand NextCommand()
        {
            PriorityCommand command = null;
            m_mutex.WaitOne();
            for (int i = 0; i < Limit; ++i)
            {
                if (m_commands[i].Count > 0)
                {
                    command = m_commands[i].Dequeue();
                    m_running.Add(command);
                    break;
                }
            }
            m_mutex.ReleaseMutex();
            return command;
        }

        public void Complete(PriorityCommand command)
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
				threads[t].Name = t.ToString("DL0000");
                threads[t].Start(this);
            }

            for (int t = 0; t < tc; ++t)
            {
                threads[t].Join();
            }
        }

        private static void RunThread(object cq)
        {
            CommandPriorityQueue workSource = cq as CommandPriorityQueue;
            if (workSource!=null)
            {
                bool running = true;
                while ((!workSource.IsComplete) && running)
                {
                    PriorityCommand next = workSource.NextCommand();
                    if (next != null)
                    {
                        running = next.Execute();
                        workSource.Complete(next);
                    }
                    else
                    {
                        Thread.Sleep(1000);
                    }
                }
            }
        }

    }
}
