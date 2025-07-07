//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! ButtonActionProgressBar, abstract base class that  sets up a button
// with a ProgressBar
//
//! Author:     Alan MacAree
//! Created:    07 Nov 2022
//----------------------------------------------------------------------

using System.Windows;
using System.Windows.Controls;

namespace Launcher
{
    /// <summary>
    /// Abstract base class for all dynamic button text and 
    /// associated actions with progress bar.
    /// </summary>
    public abstract class ButtonActionProgressBar : ButtonAction
    {
        /// <summary>
        /// Constrcutor
        /// </summary>
        /// <param name="_launcherWindow">The LauncherWindow to hold, should not be null.</param>
        public ButtonActionProgressBar( LauncherWindow _launcherWindow )
            : base( _launcherWindow )
        {
        }

        /// <summary>
        /// Sets the button style to c_launcherButtonStyle
        /// </summary>
        public override void SetupButton( Button _button )
        {
            if ( _button != null )
            {
                _button.Style = (Style)((Application.Current).FindResource( c_launcherProgressButtonStyle ));
            }
        }

        /// <summary>
        /// Our style we use that includes a ProgressBar
        /// </summary>
        private const string c_launcherProgressButtonStyle = "ProgressButtonStyle";
    }
}