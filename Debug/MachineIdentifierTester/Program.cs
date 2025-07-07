using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using FrontierSupport;

namespace MachineIdentifierTester
{
    class Program
    {
        class ConsoleReporter : FrontierSupport.FrontierMachineIdentifier.Reporter
        {
            StreamWriter m_streamWriter;

            public ConsoleReporter()
            {
                m_streamWriter = new StreamWriter("MachineIdentifierTest.txt");
            }

            public override void Output(String message)
            {
                m_streamWriter.WriteLine(message);
                m_streamWriter.Flush();
            }
        }

        static void Main(string[] args)
        {
            FrontierSupport.FrontierMachineIdentifier ident = new FrontierSupport.FrontierMachineIdentifier();

            ConsoleReporter console = new ConsoleReporter();
            ident.SetReporter(console);
            ident.GetMachineIdentifier();
        }
    }
}
