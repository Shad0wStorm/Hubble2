using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using ClientSupport;

namespace ClientSupport.Tests.ProjectUpdater
{
    class TestCommand : ClientSupport.ProjectUpdater.Command
    {
        private static int CurrentCreateIndex = 0;
        private static int CurrentExecuteIndex = 0;
        public int CreateIndex;
        public int ExecuteIndex;
        protected int m_delay;

        public TestCommand(int workingTime = 0)
        {
            CreateIndex = Interlocked.Increment(ref CurrentCreateIndex);
            m_delay = workingTime;
        }

        public static void Reset()
        {
            CurrentCreateIndex = 0;
            CurrentExecuteIndex = 0;
        }

        public virtual bool Execute()
        {
            Thread.Sleep(m_delay);
            ExecuteIndex = Interlocked.Increment(ref CurrentExecuteIndex);
            return true;
        }
    }

    class TestCreateCommand : TestCommand
    {
        public List<TestCommand> Commands = new List<TestCommand>();

        private ClientSupport.ProjectUpdater.CommandQueue m_queue;

        public TestCreateCommand(ClientSupport.ProjectUpdater.CommandQueue queue, int workingTime = 0)
            : base(workingTime)
        {
            m_queue = queue;
        }

        public override bool Execute()
        {
            foreach (ClientSupport.ProjectUpdater.Command c in Commands)
            {
                Thread.Sleep(m_delay);
                m_queue.AddCommand(c);
            }
            base.Execute();
            return true;
        }
    }

    [TestClass]
    public class CommandQueueTests
    {
        [TestInitialize]
        public void SetUp()
        {
            TestCommand.Reset();
        }

        /// <summary>
        /// Helper method to populate a command queue.
        /// </summary>
        /// <param name="commands">Number of commands to add to the queue.</param>
        /// <param name="index">Index of the command to be returned via the command parameter.</param>
        /// <param name="command">Command added to the queue at the indexed position.</param>
        /// <returns>The populated command queue.</returns>
        private ClientSupport.ProjectUpdater.CommandQueue PopulateCommandQueue
        (
            int commands,
            int index,
            out TestCommand command
        )
        {
            ClientSupport.ProjectUpdater.CommandQueue queue = new ClientSupport.ProjectUpdater.CommandQueue();

            command = null;
            for (int c = 0; c < commands; ++c)
            {
                TestCommand addCommand = new TestCommand();
                if (c == index)
                {
                    command = addCommand;
                }
                queue.AddCommand(addCommand);
            }

            return queue;
        }

        class GeneratedCommandList
        {
            public List<ClientSupport.ProjectUpdater.Command> m_commands;
            public ClientSupport.ProjectUpdater.CommandQueue m_queue;
        };

        private GeneratedCommandList GenerateCommandList()
        {
            GeneratedCommandList result = new GeneratedCommandList();
            result.m_commands = new List<ClientSupport.ProjectUpdater.Command>();
            result.m_queue = new ClientSupport.ProjectUpdater.CommandQueue();

            int[] timings = { 50, 20, 10, 50, 20 };

            foreach (int t in timings)
            {
                TestCommand add;
                add = new TestCommand(t);
                result.m_queue.AddCommand(add);
                result.m_commands.Add(add);
            }

            return result;
        }

        [TestMethod]
        public void CommandQueueEmpty()
        {
            TestCommand command;
            ClientSupport.ProjectUpdater.CommandQueue empty = PopulateCommandQueue(0, 0, out command);
            Assert.IsTrue(empty.IsEmpty);
        }

        [TestMethod]
        public void CommandQueueWithCommandNotEmpty()
        {
            TestCommand command;
            ClientSupport.ProjectUpdater.CommandQueue item = PopulateCommandQueue(1,0, out command);
            Assert.IsFalse(item.IsEmpty);
        }

        [TestMethod]
        public void RetrieveExpectedSingleCommandFromQueue()
        {
            TestCommand command;
            ClientSupport.ProjectUpdater.CommandQueue item = PopulateCommandQueue(1, 0, out command);

            Assert.AreEqual(command, item.NextCommand());
        }

        [TestMethod]
        public void RetrieveExpectedSingleCommandFromQueueLeavesQueueEmpty()
        {
            TestCommand command;

            ClientSupport.ProjectUpdater.CommandQueue item = PopulateCommandQueue(1, 0, out command);
            item.NextCommand();

            Assert.IsTrue(item.IsEmpty);
        }

        [TestMethod]
        public void RetrieveFirstCommandFromQueueGetsExpectedCommand()
        {
            TestCommand command;

            ClientSupport.ProjectUpdater.CommandQueue item = PopulateCommandQueue(3, 0, out command);
            Assert.AreEqual(command, item.NextCommand());
        }

        [TestMethod]
        public void RetrieveFirstCommandFromQueueLeavesAdditionalCommandsOnQueue()
        {
            TestCommand command;

            ClientSupport.ProjectUpdater.CommandQueue item = PopulateCommandQueue(3, 0, out command);
            Assert.IsFalse(item.IsEmpty);
        }

        [TestMethod]
        public void CommandQueueSingleThreadEmptiesQueue()
        {
            GeneratedCommandList gcl = GenerateCommandList();

            gcl.m_queue.Process(1);

            Assert.IsTrue(gcl.m_queue.IsEmpty);
        }

        [TestMethod]
        public void CommandQueueSingleThreadQueueComplete()
        {
            GeneratedCommandList gcl = GenerateCommandList();

            gcl.m_queue.Process(1);

            Assert.IsTrue(gcl.m_queue.IsComplete);
        }

        [TestMethod]
        public void CommandQueueSingleThreadExpectedOrder()
        {
            GeneratedCommandList gcl = GenerateCommandList();

            gcl.m_queue.Process(1);

            foreach (ClientSupport.ProjectUpdater.Command command in gcl.m_commands)
            {
                TestCommand tc = command as TestCommand;
                Assert.IsNotNull(tc);
                Assert.AreEqual(tc.CreateIndex, tc.ExecuteIndex);
            }
        }

        [TestMethod]
        public void CommandQueueTwoThreadsEmptiesQueue()
        {
            GeneratedCommandList gcl = GenerateCommandList();

            gcl.m_queue.Process(2);

            Assert.IsTrue(gcl.m_queue.IsEmpty);
        }

        [TestMethod]
        public void CommandQueueTwoThreadsQueueComplete()
        {
            GeneratedCommandList gcl = GenerateCommandList();

            gcl.m_queue.Process(2);

            Assert.IsTrue(gcl.m_queue.IsComplete);
        }

        [TestMethod]
        public void CommandQueueTwoThreadsExpectedOrder()
        {
            GeneratedCommandList gcl = GenerateCommandList();

            gcl.m_queue.Process(2);

            // This relies on the timings used being sufficiently course that
            // we can accurately predict the behaviour on multiple threads.
            //          delays { 50, 20, 10, 50, 20 };
            // Time  0 : Thread 1 Picks up work item 1 (50).
            // Time  0 : Thread 2 Picks up work item 2 (20).
            // Time 20 : Thread 2 Completes work item 2.
            // Time 20 : Thread 2 Picks up work item 3 (10).
            // Time 30 : Thread 2 Completes work item 3.
            // Time 30 : Thread 2 Picks up work item 4 (50).
            // Time 50 : Thread 1 Completes work item 1.
            // Time 50 : Thread 1 Picks up work item 5 (20).
            // Time 70 : Thread 1 Completes work item 5.
            // Time 80 : Thread 2 Completes work item 4.
            //
            // Work items complete in the order 2, 3, 1, 5, 4
            int[] prediction = { 3, 1, 2, 5, 4 };

            for (int ci = 0; ci < gcl.m_commands.Count; ++ci)
            {
                TestCommand tc = gcl.m_commands[ci] as TestCommand;
                Assert.IsNotNull(tc);
                Assert.AreEqual(prediction[ci], tc.ExecuteIndex);
            }
        }

        [TestMethod]
        public void CommandCanAddNewCommands()
        {
            ClientSupport.ProjectUpdater.CommandQueue queue = new ClientSupport.ProjectUpdater.CommandQueue();
            TestCreateCommand parent = new TestCreateCommand(queue, 20);
            TestCommand first = new TestCommand(30);
            parent.Commands.Add(first);
            TestCommand second = new TestCommand(20);
            parent.Commands.Add(second);
            TestCommand third = new TestCommand(10);
            parent.Commands.Add(third);

            queue.AddCommand(parent);

            queue.Process(1);

            Assert.IsTrue(queue.IsEmpty);
            Assert.IsTrue(queue.IsComplete);

            Assert.AreEqual(1, parent.CreateIndex);
            Assert.AreEqual(1, parent.ExecuteIndex);

            Assert.AreEqual(2, first.CreateIndex);
            Assert.AreEqual(2, first.ExecuteIndex);

            Assert.AreEqual(3, second.CreateIndex);
            Assert.AreEqual(3, second.ExecuteIndex);

            Assert.AreEqual(4, third.CreateIndex);
            Assert.AreEqual(4, third.ExecuteIndex);
        }

        [TestMethod]
        public void CommandCanAddNewCommandsParallel()
        {
            //System.Diagnostics.Debugger.Break();
            TestCommand.Reset();
            ClientSupport.ProjectUpdater.CommandQueue queue = new ClientSupport.ProjectUpdater.CommandQueue();
            TestCreateCommand parent = new TestCreateCommand(queue, 20);
            TestCommand first = new TestCommand(35);
            parent.Commands.Add(first);
            TestCommand second = new TestCommand(20);
            parent.Commands.Add(second);
            TestCommand third = new TestCommand(10);
            parent.Commands.Add(third);

            queue.AddCommand(parent);

            queue.Process(2);

            // Time         Thread 1                    Thread 2
            //   0          parent executes             no work, sleep 50
            //  20          parent adds 1st to queue    ---
            //  40          parent adds 2nd to queue    ---
            //  50          ---                         first executes
            //  60          parent adds 3rd to queue
            //  80          parent completes            ---
            //              second executes             ---
            //  85          ---                         first completes
            //              ---                         third executes
            //  95          ---                         third completes
            // 100          second completes            ---
            //
            // Note: added items can be executed as soon as they are added to
            //       the queue so do not add them until any requirements are
            //       met by the parent, e.g. necessary files are created.

            Assert.IsTrue(queue.IsEmpty);
            Assert.IsTrue(queue.IsComplete);

            Assert.AreEqual(1, parent.CreateIndex);
            Assert.AreEqual(1, parent.ExecuteIndex);

            Assert.AreEqual(2, first.CreateIndex);
            Assert.AreEqual(2, first.ExecuteIndex);

            Assert.AreEqual(3, second.CreateIndex);
            Assert.AreEqual(4, second.ExecuteIndex);

            Assert.AreEqual(4, third.CreateIndex);
            Assert.AreEqual(3, third.ExecuteIndex);
        }
    }
}
