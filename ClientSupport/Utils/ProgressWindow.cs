using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientSupport.Utils
{
    class ProgressWindow
    {
        private double m_windowSize;
        private int m_startIndex;
        private int m_endIndex;
        private double m_totalTime;
        private Int64 m_totalQuantity;
        struct Entry {
            public Int64 m_quantity;
            public double m_time;
        }
        Entry[] m_progress;

        private double m_rate = 0;
        public double Rate
        {
            get { return m_rate; }
        }

        private double m_rateMB = 0;
        public double RateMB
        {
            get { return m_rateMB; }
        }

        public double TotalSeconds
        {
            get
            {
                if (m_totalQuantity == 0)
                {
                    return 0;
                }
                return m_totalTime/1000.0;
            }
        }

        public double TotalQuantity
        {
            get
            {
                if (m_totalQuantity == 0)
                {
                    return 1;
                }
                return m_totalQuantity;
            }
        }

        /// <summary>
        /// Use the constructor to set the window size in seconds.
        /// </summary>
        /// <param name="windowSize"></param>
        public ProgressWindow(double windowSize)
        {
            m_progress = new Entry[65536];
            m_windowSize = windowSize * 1000.0; 
            m_startIndex = 0;
            m_endIndex = 0;
            m_totalTime = 0;
            m_totalQuantity = 0;
        }



        /// <summary>
        /// Add the passed sample to the window and update the totals.
        /// </summary>
        /// <param name="quantity">Number of units of progress made.</param>
        /// <param name="time">Time in MS the progress took.</param>
        public void AddSample(Int64 quantity, double time)
        {
            m_progress[m_endIndex].m_quantity = quantity;
            m_progress[m_endIndex].m_time = time;
            m_totalTime += time;
            m_totalQuantity += quantity;
            while ((m_totalTime > m_windowSize) && (m_startIndex!=m_endIndex))
            {
                m_totalTime = m_totalTime - m_progress[m_startIndex].m_time;
                m_totalQuantity = m_totalQuantity - m_progress[m_startIndex].m_quantity;
                m_startIndex = (m_startIndex + 1) % m_progress.Length;
            }
            m_endIndex = m_endIndex + 1;
            m_endIndex = m_endIndex % m_progress.Length;
            if (m_endIndex == m_startIndex)
            {
                // Caught up with the start pointer so drop the oldest item
                // and to leave space with the next one and move the
                // start pointer along. This avoids resizing the buffer but
                // means that if we have a lot of very small time steps we
                // will calculate the average of a smaller window than planned.
                m_totalTime = m_totalTime - m_progress[m_startIndex].m_time;
                m_totalQuantity = m_totalQuantity - m_progress[m_startIndex].m_quantity;
                m_startIndex = (m_startIndex + 1) % m_progress.Length;
            }
            if (m_totalTime > 0)
            {
                m_rate = (1000.0 * m_totalQuantity) / m_totalTime;
                m_rateMB = (m_rate) / (1024 * 1024);
            }
        }
    }
}
