//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! ButtonAction, abstract base class for all dynamic button text and 
// associated actions.
//
//! Author:     Alan MacAree
//! Created:    09 Sept 2022
//----------------------------------------------------------------------

using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Launcher
{
    /// <summary>
    /// Abstract base class for all dynamic button text and 
    /// associated actions.
    /// </summary>
    public abstract class ButtonAction
    {
        /// <summary>
        /// Constrcutor
        /// </summary>
        /// <param name="_launcherWindow">The LauncherWindow to hold, should not be null.</param>
        public ButtonAction( LauncherWindow _launcherWindow)
        {
            Debug.Assert( _launcherWindow != null );
            m_launcherWindow = _launcherWindow;
        }

        /// <summary>
        /// Returns the LauncherWindow object
        /// </summary>
        /// <returns>The LauncherWindow</returns>
        protected LauncherWindow GetLauncherWindow()
        {
            return m_launcherWindow;
        }

        /// <summary>
        /// The derived class should return the button text for the specific action.
        /// </summary>
        /// <returns>The string to display on a button for this action</returns>
        public abstract string ButtonText();

        /// <summary>
        /// The derived class should perform the specific action.
        /// </summary>
        /// <param name="_sender">The object that caused this action</param>
        /// <returns>True if the action was ran without error</returns>
        public abstract bool PerformAction( object _sender );

        /// <summary>
        /// Allows the button to setup anything special regarding the
        /// button, e.g. styles etc.
        /// </summary>
        public virtual void SetupButton( Button _button )
        {
            if ( _button != null )
            {
                _button.Style = (Style)((Application.Current).FindResource( c_launcherButtonStyle ));
            }
        }

        // Our LauncherWindow 
        private LauncherWindow m_launcherWindow;

        private const string c_launcherButtonStyle = "LauncherButtonStyle";
    }
}
