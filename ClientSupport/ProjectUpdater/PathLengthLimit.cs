using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientSupport.ProjectUpdater
{
    class PathLengthLimit
    {
        private const int c_limit = 255;

        private int m_base;
        private int m_max;

        private bool PathTooLong
        {
            get
            {
                return m_max > c_limit;
            }
        }

        public PathLengthLimit(String dirname)
        {
            m_base = dirname.Length;
        }

        public bool Include(String filename)
        {
            int length = m_base + filename.Length;
            if (length > m_max)
            {
                m_max = length;
            }
            return PathTooLong;
        }
    }
}
