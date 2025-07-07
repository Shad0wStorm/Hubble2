//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! LogEventInterface, allows the UserControls to send log ininformation
//!                     to be stored.
//
//! Author:     Alan MacAree
//! Created:    11 Oct 2022
//----------------------------------------------------------------------

namespace FDUserControls
{
    /// <summary>
    /// Classes that provide this interface must log the events that are
    /// passed to it, either to a local log ot to a server log.
    /// </summary>
    public interface ILogEvent
    {
        /// <summary>
        /// Logs an action
        /// </summary>
        /// <param name="_action"></param>
        void LogEvent( string _action );

        /// <summary>
        /// Logs an action, along with a key and a description
        /// </summary>
        /// <param name="_action">The name of the action to log</param>
        /// <param name="_key">The key describing the description</param>
        /// <param name="_description">The description of the log</param>
        void LogEvent( string _action, string _key, string _description );
    }
}
