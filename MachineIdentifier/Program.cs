using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Win32;

namespace MachineIdentifier
{
    class Program
    {
        static int Main(string[] args)
        {
            const String c_msNodeName = @"SOFTWARE\\Microsoft\\Cryptography";
            const String c_valueName = @"MachineGuid";
            String identifier = null;
            RegistryKey key = Registry.LocalMachine.OpenSubKey(c_msNodeName);
            if (key != null)
            {
                object obID = key.GetValue(c_valueName);
                if (obID != null)
                {
                    identifier = obID.ToString();
                }
            }
            if (identifier == null)
            {
                return 1;
            }

            char[] chars = identifier.ToCharArray();
            int total = 0;
            foreach (char ch in chars)
            {
                total += Convert.ToInt32(ch);
            }
            total = total % 256;
            identifier = identifier + String.Format("{0:x2}", total);
            Console.WriteLine(identifier);
            return 0;
        }
    }
}
