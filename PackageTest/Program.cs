using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FilePackage;

namespace PackageTest
{
    class Program
    {
        /// <summary>
        /// This program repeatedly creates a zip file containing two files
        /// using the FilePackage module used by the CrashReporter to send
        /// crash dumps to the server.
        /// 
        /// The file is then read back and the content file names listed.
        /// Contents are not actually extracted since the error being
        /// investigated results in zip files that can not be opened. As a
        /// result the program exits when a zip file fails to open.
        /// 
        /// The program is started with one or two parameters.
        /// 
        /// The first parameter is the maximum size of the large data file to
        /// create in bytes. The size of the smaller file also varies based on
        /// the length of the larger file, but is always less than 2KB.
        /// 
        /// The second parameter is the minimum size of the large data file to
        /// create in bytes, and defaults to 0.
        /// 
        /// The test always creates the largest files first, both files
        /// containing a random series of bytes. Each test cycle the size is
        /// reduced by a byte until the lower limit is reached.
        /// </summary>
        /// <param name="args">Command line arguments: &lt;limit&gt; &lt;lowerlimit&gt;</param>
        static void Main(string[] args)
        {
            String zipFile = "Test.zip";

            int limit = int.Parse(args[0]);
            int lowerLimit = 0;
            if (args.Length > 1)
            {
                lowerLimit = int.Parse(args[1]);
            }
            int current = limit;
            bool running = true;
            while (running && (current>=lowerLimit))
            {
                Console.WriteLine("Processing file of size " + current.ToString());
                GenerateFile(current, "Gen.txt");
                GenerateFile(current % 1910, "Gen2.txt");
                WritePackage(zipFile);
                running = ReadPackage(zipFile);

                current--;
            }
        }

        static void GenerateFile(int size, String name)
        {
            Random rnd = new Random();
            byte[] bytes = new byte[256];
            using (BinaryWriter br = new BinaryWriter(File.Open(name, FileMode.Create)))
            {
                for (int b = 0; b < size; b++)
                {
                    int offset = b % 256;
                    if (offset == 0)
                    {
                        rnd.NextBytes(bytes);
                    }
                    br.Write(bytes[offset]);
                }
            }
        }

        static void WritePackage(String target)
        {
            FilePackage.FilePackage package = new FilePackage.FilePackage();
            package.AddFile("Gen.txt", "Crash.dmp");
            package.AddFile("Gen2.txt", "spec.xml");

            package.WriteFile(target);
        }

        static bool ReadPackage(string source)
        {
            try
            {
                using (Package package = Package.Open(source, FileMode.Open, FileAccess.Read))
                {
                    PackagePartCollection parts = package.GetParts();
                    foreach (PackagePart part in parts)
                    {
                        Console.WriteLine("Package contained " + part.Uri.ToString());
                    }
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("Exception processing file : " + source);
                Console.WriteLine(ex.Message);
                return false;
            }
            return true;
        }
    }
}
