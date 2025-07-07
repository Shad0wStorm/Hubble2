using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Forms;

namespace Elevate
{
    class Program
    {
        static void Main(string[] args)
        {
            ProcessStartInfo startInfo;
            startInfo = new ProcessStartInfo();
            startInfo.FileName = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().GetName().Name), "EDLaunch.exe");
            startInfo.UseShellExecute = true;
            startInfo.Verb = "runas";
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;

            try
            {
                Process.Start(startInfo);
            }
            catch (Win32Exception ex)
            {
                if (ex.ErrorCode != 1223)
                {
                    MessageBox.Show("Failed to start launcher with elevated privileges.", "EDLaunch");
                }
            }
        }
    }
}
