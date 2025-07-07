using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientSupport.Utils
{
    class BufferManager
    {
        private int m_bufferSize;
        public int Size { get { return m_bufferSize; } }

        private Byte[] m_data;
        public Byte[] Data { get { return m_data; } }

        public BufferManager(long maxSize)
        {
            const int minBufferSize = 64 * 1024;
            m_bufferSize = minBufferSize;
            if (maxSize > minBufferSize)
            {
                const int maxBufferSize = 32 * 1024 * 1024;
                while (m_bufferSize < maxBufferSize)
                {
                    if ((maxSize / m_bufferSize) < 512)
                    {
                        break;
                    }
                    m_bufferSize = m_bufferSize * 2;
                }
            }
            m_data = new Byte[m_bufferSize];
        }
    }
}
