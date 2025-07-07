using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

using ClientSupport;

namespace ManifestTool
{
    public class ManifestFileBuilder : ManifestFile
    {
        public String Error = "";

        public ManifestFileBuilder(String path)
        {
            m_fileName = path;
        }

        public bool AddFile(String file, String root)
        {
            ManifestEntry entry = new ManifestEntry();

            if (file.StartsWith(root))
            {
                entry.Path = file.Substring(root.Length+1);
            }
            else
            {
                Error += "Added file does not match root location:\nFile: " + file + "\nRoot: " + root+"\n\n";
                return false;
            }

            DecoderRing ring = new DecoderRing();
            long length;
            entry.Hash = ring.SHA1EncodeFile(file, out length);
            entry.Size = length;

            foreach (ManifestEntry testentry in m_files.Values)
            {
                if (testentry.Hash == entry.Hash)
                {
                    if (testentry.Size != entry.Size)
                    {
                        Error += "Attempting to add file " + entry.Path + " when existing file " + testentry.Path + " has the same hash " + entry.Hash + " but different size (" + entry.Size.ToString() + " vs " + testentry.Size.ToString() + ")";
                        return false;
                    }
                }
            }
            m_files.Add(entry.Path, entry);

            return true;
        }

        public void SaveFile(String path, String title, String version, bool slash)
        {
            String filepath = Path.Combine(path,m_fileName);

            ManifestEntry[] entries = m_files.Values.ToArray();

            // The following can be used to modify the order in which entries
            // are written to the file since this can make a difference to the
            // apparent download speed.
            //
            // Experimentation has shown no particular benefit so it remains
            // disabled for now.

            // Sort the entries in increasing size so small files are processed
            // first. This seemed to give a minor reduction in the time taken
            // for the launcher to process the manifest, but processing the
            // smaller entries initial estimates make the download seem to be
            // taking a very long time potentially causing people to give up.
            //Array.Sort(entries);

            // The following assume that the entries have been previously
            // sorted see the method descriptions for more information.
            //entries = Segment(entries, 16);
            //entries = BothEnds(entries, 15, true);

            using (XmlWriter writer = XmlWriter.Create(filepath))
            {
                writer.WriteStartElement("Manifest");
                writer.WriteAttributeString("title", title);
                writer.WriteAttributeString("version", version);

                foreach (ManifestEntry entry in entries)
                {
                    writer.WriteStartElement("File");
                    String savePath = entry.Path;
                    if (slash)
                    {
                        savePath = savePath.Replace("\\", "/");
                    }
                    writer.WriteElementString("Path", savePath);
                    writer.WriteElementString("Hash", entry.Hash);
                    writer.WriteElementString("Size", entry.Size.ToString());

                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }
        }

        /// <summary>
        /// Interleave segments of the manifest entry array.
        /// 
        /// Split the entry array into segmentCount equal segments (the final
        /// segment may be smaller if the total number of entries is not
        /// divisible by the number of segments).
        /// 
        /// One element is taken from each segment in turn until all entries
        /// are included.
        /// 
        /// The aim is to provide a mix of large and small files to even out
        /// the download rate somewhat.
        /// </summary>
        /// <param name="entries"></param>
        /// <param name="segmentCount"></param>
        /// <returns></returns>
        private ManifestEntry[] Segment(ManifestEntry[] entries, int segmentCount)
        {
            int length = entries.Length;
            ManifestEntry[] resort = new ManifestEntry[length];

            int segmentSize = length / segmentCount;
            if (segmentSize * segmentCount < length)
            {
                ++segmentSize;
            }
            int target = 0;
            int pass = 0;
            for (int i = 0; i < length; ++i)
            {
                resort[i] = entries[target];
                target = target + segmentSize;
                if (target >= length)
                {
                    ++pass;
                    target = pass;
                }
            }

            return resort;
        }

        /// <summary>
        /// Take files from the beginning and end of the entries.
        /// 
        /// The bias value indicates how many entries to take from the front
        /// of the list for each item taken from the back. It is assumed the
        /// input list is sorted smallest to highest.
        /// 
        /// The objective is to balance the large and small files to avoid
        /// a large number of small files grouping together and indicating a
        /// low download rate due to the per file overhead being high relative
        /// to the actual download speed.
        /// 
        /// Precise effects will depend on the range and mean of sizes in the
        /// entries.
        /// </summary>
        /// <param name="entries">
        /// List of entries to rearrange should be sorted smallest to largest.
        /// </param>
        /// <param name="bias">
        /// Number of items from the start of the list for each item taken from
        /// the back.
        /// </param>
        /// <param name="reverse">
        /// Reverse the output list so that the items closest in size come at
        /// the start of the result rather than the end.
        /// </param>
        /// <returns></returns>
        private ManifestEntry[] BothEnds(ManifestEntry[] entries, int bias, bool reverse)
        {
            int length = entries.Length;
            ManifestEntry[] generate = new ManifestEntry[length];
            int step = bias + 1;
            int start = 0;
            int end = length - 1;
            for (int i = 0; i < length; ++i)
            {
                int j = reverse ? length - 1 - i : i;
                if ((i % step) == 0)
                {
                    generate[j] = entries[end--];
                }
                else
                {
                    generate[j] = entries[start++];
                }
            }
            return generate;
        }
    }
}
