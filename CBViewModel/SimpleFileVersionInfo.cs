using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CBViewModel
{
    public class SimpleFileVersionInfo
    {
        public int FileMajorPart;
        public int FileMinorPart;
        public int FileBuildPart;
        public int FilePrivatePart;
        public SimpleFileVersionInfo(String version)
        {
            String[] segments = version.Split('.');
            if (segments.Length != 4)
            {
                FileMajorPart = -1;
                FileMinorPart = -1;
                FileBuildPart = -1;
                FilePrivatePart = -1;
            }
            else
            {
                FileMajorPart = int.Parse(segments[0]);
                FileMinorPart = int.Parse(segments[1]);
                FileBuildPart = int.Parse(segments[2]);
                FilePrivatePart = int.Parse(segments[3]);
            }
        }

        public static bool operator >(SimpleFileVersionInfo left, SimpleFileVersionInfo right)
        {
            if (left.FileMajorPart > right.FileMajorPart)
            {
                return true;
            }
            else
            {
                if (left.FileMajorPart < right.FileMajorPart)
                {
                    return false;
                }
            }
            if (left.FileMinorPart > right.FileMinorPart)
            {
                return true;
            }
            else
            {
                if (left.FileMinorPart < right.FileMinorPart)
                {
                    return false;
                }
            }
            if (left.FileBuildPart > right.FileBuildPart)
            {
                return true;
            }
            else
            {
                if (left.FileBuildPart < right.FileBuildPart)
                {
                    return false;
                }
            }
            if (left.FilePrivatePart > right.FilePrivatePart)
            {
                return true;
            }

            return false;
        }

        public static bool operator <(SimpleFileVersionInfo left, SimpleFileVersionInfo right)
        {
            if (left.FileMajorPart < right.FileMajorPart)
            {
                return true;
            }
            else
            {
                if (left.FileMajorPart > right.FileMajorPart)
                {
                    return false;
                }
            }
            if (left.FileMinorPart < right.FileMinorPart)
            {
                return true;
            }
            else
            {
                if (left.FileMinorPart > right.FileMinorPart)
                {
                    return false;
                }
            }
            if (left.FileBuildPart < right.FileBuildPart)
            {
                return true;
            }
            else
            {
                if (left.FileBuildPart > right.FileBuildPart)
                {
                    return false;
                }
            }
            if (left.FilePrivatePart < right.FilePrivatePart)
            {
                return true;
            }

            return false;
        }
    }
}
