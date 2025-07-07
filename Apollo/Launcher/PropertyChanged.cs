//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! PropertyChangedBase, used to fire property change events.
//
//! Author:     Alan MacAree
//! Created:    08 Dec 2022
//----------------------------------------------------------------------

using System.ComponentModel;

namespace Launcher
{
    /// <summary>
    /// Used to fire property change events.
    /// </summary>
    public class PropertyChangedBase : INotifyPropertyChanged
    {
        /// <summary>
        /// Our PropertyChangedevent
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the event
        /// </summary>
        /// <param name="_propertyName">The name of the event to raise</param>
        protected void PropertyChange( string _propertyName = "" )
        {
            PropertyChangedEventHandler handle = PropertyChanged;
            if ( handle != null )
            {
                handle( this, new PropertyChangedEventArgs( _propertyName ) );
            }
        }
    }
}
