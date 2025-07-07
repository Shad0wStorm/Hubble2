using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Security.Principal;
using System.Text;

using Microsoft.Win32;

using ClientSupport;

namespace FrontierSupport
{
    public class FrontierMachineIdentifier : MachineIdentifierInterface
    {
        String m_identifier;
        const String c_nodeName = @"SOFTWARE\\Frontier Developments\\Cryptography";
        const String c_msNodeName = @"SOFTWARE\\Microsoft\\Cryptography";
        const String c_valueName = @"MachineGuid";

        public abstract class Reporter
        {
            public abstract void Output(String message);
        }

        private Reporter m_reporter;

        public FrontierMachineIdentifier()
        {
            m_identifier = null;
        }

        public void SetReporter(Reporter reporter)
        {
            m_reporter = reporter;
        }

        [ConditionalAttribute("DEVELOPMENT")]
        private void Output(String message)
        {
            if (m_reporter != null)
            {
                m_reporter.Output(message);
            }
        }

        private void SetIdentifier()
        {
            ReadIdentifierFromRegistry();
            if (m_identifier == null)
            {
                if (GenerateIdentifierAsRequired())
                {
                    ReadIdentifierFromRegistry();
                }
            }
        }

        private void ReadIdentifierFromRegistry()
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(c_nodeName);

            String frontierIdentifier = null;
            String msIdentifier = null;

            if (key != null)
            {
                Output("PASS : Found Frontier Identifier Node");
                object obID = key.GetValue(c_valueName);
                if (obID != null)
                {
                    Output("PASS : Found Frontier Identifier Value");
                    frontierIdentifier = obID.ToString();
                }
                else
                {
                    Output("FAIL : Found Frontier Identifier Value");
                }
            }
            else
            {
                Output("FAIL : Found Frontier Identifier Node");
            }

            /*
            RegistryKey lm64 = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, RegistryView.Registry64);
            if (lm64!=null)
            {
                key = lm64.OpenSubKey(c_msNodeName);
                if (key != null)
                {
                    object msID = key.GetValue(c_valueName);
                    if (msID != null)
                    {
                        msIdentifier = msID.ToString();
                    }
                }
            }
             * */
            key = Registry.LocalMachine.OpenSubKey(c_msNodeName);
            if (key != null)
            {
                Output("PASS : Found MS Identifier Node");
                object obID = key.GetValue(c_valueName);
                if (obID != null)
                {
                    Output("PASS : Found MS Identifier Value");
                    msIdentifier = obID.ToString();
                }
                else
                {
                    Output("FAIL : Found MS Identifier Value");
                }
            }
            else
            {
                Output("FAIL : Found MS Identifier Node");
            }
            if (msIdentifier == null)
            {
                // Failed to read the value that really should be there.
                // Try the hacky approach.
                msIdentifier = ExternalIdentifierScan();
            }
            if ((msIdentifier!=null) && (frontierIdentifier!=null))
            {
                DecoderRing ring = new DecoderRing();
                m_identifier = ring.SHA1Encode(msIdentifier + frontierIdentifier, 16);
                Output("PASS : Generated full machine ID");
            }
            else
            {
                Output("FAIL : Generated full machine ID");
            }
        }

        private bool GenerateIdentifierAsRequired()
        {
            Output("No existing key found, attempting to generate a new one.");
            RegistryKey key = Registry.CurrentUser.OpenSubKey(c_nodeName);
            if (key != null)
            {
                Output("PASS : Found Frontier Identifier Node");
                object id = key.GetValue(c_valueName);
                if (id != null)
                {
                    Output("FAIL : Frontier Identifier Node missing");
                    // Key already exists.
                    return true;
                }
                else
                {
                    Output("PASS : Frontier Identifier Value missing");
                }
            }
            else
            {
                Output("PASS : Frontier Identifier Node Missing");
            }
            // Try reopening in writable mode
            try
            {
                key = Registry.CurrentUser.CreateSubKey(c_nodeName, RegistryKeyPermissionCheck.ReadWriteSubTree);
                if (key != null)
                {
                    Output("PASS : Frontier Identifier Node Created : Setting Value");
                    key.SetValue(c_valueName, Guid.NewGuid().ToString());
                    return true;
                }
                else
                {
                    Output("FAIL : Frontier Identifier Node Created : Unable to set value.");
                }
            }
            catch (System.Exception ex)
            {
                Output("EXCP : Creating Frontier Identifier Node : Details "+ex.Message);
                // reporting failure anyway.
            }
            return false;
        }

        /// <summary>
        /// A small subset of machines is hitting a problem where the launcher
        /// appears to be running as a 32bit process even though it is set as
        /// AnyCPU running on a 64bit system. For some reason the MachineGUID
        /// value does not seem to be reflected by WOW64 correctly so while it
        /// is visible to 64bit applications it is not visible from 32bit when
        /// running on 64bit.
        /// 
        /// There are some pretty horrible workarounds involving P/Invoke to
        /// directly call the associated DLLs. If we were using .NET 4.0+ then
        /// there is a managed solution for reading from specific registry
        /// views which would also work. Unfortunately that would require an
        /// update to .NET 4.0 which was not installed as standard on Win7.
        /// 
        /// Both of these seem like a lot of effort for something which may not
        /// work anyway, it is possible that even if the launcher can uncover
        /// the id the game will still fail.
        /// 
        /// Since a process built specifically for 64bit does correctly find
        /// the id on an affected machine we therefore run an external tool
        /// to read the value and read it back. The launcher cannot be built
        /// as a 64bit only application as it needs to run on 32bit machines.
        /// </summary>
        /// <returns></returns>
        private String ExternalIdentifierScan()
        {
            Output("Looking for external tool");

            try
            {
                Assembly app = Assembly.GetEntryAssembly();

                String apppath = System.IO.Path.GetDirectoryName(app.Location);
                String idexe = System.IO.Path.Combine(apppath, "MachineIdentifier.exe");
                if (System.IO.File.Exists(idexe))
                {
                    Output("PASS : Found external tool");
                    Process p = new Process();
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.FileName = idexe;
                    Output("Running external tool");
                    p.Start();
                    String output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                    if (p.ExitCode == 0)
                    {
                        Output("PASS : Ran external tool");
                        output = output.Trim();
                        int end = output.Length - 2;
                        String ident = output.Substring(0, end);
                        String check = output.Substring(end, 2);
                        char[] chars = ident.ToCharArray();
                        int total = 0;
                        foreach (char ch in chars)
                        {
                            total += Convert.ToInt32(ch);
                        }
                        String local = String.Format("{0:x2}", total % 256);
                        if (local == check)
                        {
                            Output("PASS : Extracted identifier");
                            return ident;
                        }
                        else
                        {
                            Output("FAIL : Extracted identifier");
                        }
                    }
                    else
                    {
                        Output("FAIL : Ran external tool");
                    }
                }
                else
                {
                    Output("FAIL : Found external tool");
                }
            }
            catch (System.Exception ex)
            {
                Output("FAIL : Exception running external tool " + ex.Message);
            }
            return null;
        }

        public override String GetMachineIdentifier()
        {
            if (m_identifier == null)
            {
                SetIdentifier();
            }
            return m_identifier;
        }

        private const String c_steamKeyPrefix = "Software\\Valve\\TestApp";
        private const String c_steamKeyEntry = "SteamKey";

        public override string GetSteamKey(string ident)
        {
            String keyName = c_steamKeyPrefix + ident;

            RegistryKey key = Registry.CurrentUser.OpenSubKey(keyName);

            String keyValue = null;
            if (key != null)
            {
                object obID = key.GetValue(c_steamKeyEntry);
                if (obID != null)
                {
                    keyValue = obID.ToString();
                }
            }

            return keyValue;
        }
    }
}
