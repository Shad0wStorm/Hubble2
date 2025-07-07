//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! Debouncer
//
//! Author:     Alan MacAree
//! Created:    11 Jan 2023
//----------------------------------------------------------------------

using System;
using System.Windows.Threading;

namespace Launcher
{
    /// <summary>
    /// Starts a process in a specified amount of time, subsequent calls will override
    /// any previous calls, allowing an action to be delayed (replaced) and then starting
    /// the wait period again before actioning the process.
    /// 
    /// It is intended that this is used to reduce the load on server calls.
    /// </summary>
    class Debouncer
    {
        /// <summary>
        /// Starts (or restarts) a call to perform an action in _intervalMS time
        /// </summary>
        /// <param name="_intervalMS">The time to wait before the action is started</param>
        /// <param name="_action">The action to perform</param>
        /// <param name="_param">Any parameters to pass onto the action</param>
        public void Debounce( int _intervalMS, Action<object> _action, object _param = null )
        {
            // Restarts the timer each time this is called
            lock ( m_TimerLock )
            {
                m_timer?.Stop();
                m_timer = null;
            }

            // Run the call when the timer is up.
            m_timer = new DispatcherTimer( TimeSpan.FromMilliseconds( _intervalMS ), DispatcherPriority.ApplicationIdle, ( s, e ) =>
            {
                lock ( m_TimerLock )
                {
                    if ( m_timer != null )
                    {
                        m_timer?.Stop();
                        m_timer = null;
                    }
                }
                _action.Invoke( _param );

            }, Dispatcher.CurrentDispatcher );

            lock ( m_TimerLock )
            {
                m_timer.Start();
            }
        }

        /// <summary>
        /// Our timer
        /// </summary>
        private DispatcherTimer m_timer;

        /// <summary>
        /// Our lock to stop the DispatcherTimer 
        /// getting accessed at the same time by
        /// different threads.
        /// </summary>
        private readonly object m_TimerLock = new object();
    }
}
