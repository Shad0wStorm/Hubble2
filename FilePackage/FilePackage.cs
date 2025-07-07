using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;

namespace FilePackage
{
    public class FilePackage
    {
        private class FileHelper
        {
            public String m_source;
            public Stream m_sourceStream;
            public Uri m_path;
            public String m_mimeType;
        }

        List<FileHelper> m_files;

        public FilePackage()
        {
            Reset();
        }

        public void Reset()
        {
            m_files = new List<FileHelper>();
        }

        private FileHelper CreateFile(String storeAs)
        {
            FileHelper fileDetails = new FileHelper();

            fileDetails.m_path = PackUriHelper.CreatePartUri(new Uri(storeAs, UriKind.Relative));

            String extension = Path.GetExtension(storeAs).ToLowerInvariant();

            fileDetails.m_mimeType = System.Net.Mime.MediaTypeNames.Application.Octet;
            if (extension == ".xml")
            {
                fileDetails.m_mimeType = System.Net.Mime.MediaTypeNames.Text.Xml;
            }
            m_files.Add(fileDetails);
            return fileDetails;
        }

        public void AddFile(String path, String storeAs)
        {
            FileHelper fileDetails = CreateFile(storeAs);
            fileDetails.m_source = path;
        }

        public void AddStream(Stream source, String storeAs)
        {
            FileHelper fileDetails = CreateFile(storeAs);
            fileDetails.m_sourceStream = source;
        }

        public void WriteFile(String path)
        {
            using (Package package = Package.Open(path, FileMode.Create))
            {
                foreach (FileHelper helper in m_files)
                {
                    PackagePart part = package.CreatePart(helper.m_path, helper.m_mimeType, CompressionOption.Maximum);
                    if (helper.m_sourceStream != null)
                    {
                        CopyStream(part.GetStream(), helper.m_sourceStream);
                    }
                    else
                    {
                        using (FileStream source = new FileStream(helper.m_source, FileMode.Open, FileAccess.Read))
                        {
                            CopyStream(part.GetStream(), source);
                        }
                    }
                }
            }
        }

        private static void CopyStream(Stream destination, Stream source)
        {
            const int bufSize = 65536;
            byte[] buf = new byte[bufSize];
            int bytesRead = 0;
            while ((bytesRead = source.Read(buf, 0, bufSize)) > 0)
            {
                destination.Write(buf, 0, bytesRead);
            }
        }
    }
}
