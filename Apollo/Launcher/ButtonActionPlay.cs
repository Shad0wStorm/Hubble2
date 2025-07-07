//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! ButtonActionPlay, handles the text and action of playing a game
//
//! Author:     Alan MacAree
//! Created:    09 Sept 2022
//----------------------------------------------------------------------

using CBViewModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Launcher
{
    /// <summary>
    /// Handles the text and action of playing a game
    /// </summary>
    internal class ButtonActionPlay : ButtonAction
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="launcherWindow">The LauncherWindow object</param>
        /// <param name="_productUserCtrl">The ProductUserCtrl object</param>
        public ButtonActionPlay( LauncherWindow launcherWindow, ProductUserCtrl _productUserCtrl )
            : base( launcherWindow )
        {
            Debug.Assert( _productUserCtrl != null );
            m_productUserCtrl = _productUserCtrl;
        }

        /// <summary>
        /// The derived class should return the button text for the specific action.
        /// </summary>
        /// <returns>The string to display on a button for this action</returns>
        public override string ButtonText()
        {
            return LocalResources.Properties.Resources.BTNT_LauncherPlay;
        }

        /// <summary>
        /// The derived class should perform the specific action.
        /// </summary>
        /// <returns>True if the action was ran without error</returns>
        public override bool PerformAction( object _sender )
        {
            // we only care about specific project
            LauncherWindow launcherWindow = GetLauncherWindow();
            CobraBayView cobraBayView = launcherWindow.GetCobraBayView();
            ClientSupport.Project activeProject = cobraBayView.GetActiveProject();
            // only for Epic and only for legacy
            if (cobraBayView.IsEpic() && (activeProject.Name == "FORC-FDEV-D-1010" || activeProject.Name == "FORC-FDEV-D-1013"))
            {
                System.Windows.MessageBoxResult mbr = System.Windows.MessageBox.Show("Are you sure you want to launch the legacy version of the game? This version does not contain the latest content. Please install a Live version of the game via the 'Versions' button.", "Legacy Launch Detected", System.Windows.MessageBoxButton.YesNo);
                switch (mbr)
                {
                    case System.Windows.MessageBoxResult.Yes:
                        _ = PlayGameAsync(_sender);
                        return true;
                    case System.Windows.MessageBoxResult.No:
                        return false;
                }
                return false;
            }
            else {
                _ = PlayGameAsync(_sender);
                return true;
            }

        }

        /// <summary>
        /// Play the game async
        /// </summary>
        /// <param name="_sender">The object that caused this action</param>
        /// <returns>Task</returns>
        private async Task PlayGameAsync( object _sender )
        {
            // Disable the buttons the the launcher
            if ( m_productUserCtrl != null )
            {
                m_productUserCtrl.EnableButtons( false );
            }

            // Play the game
            await Task.Run( () =>
            {
                LauncherWindow launcherWindow = GetLauncherWindow();

                Debug.Assert( launcherWindow != null );
                if ( launcherWindow != null )
                {
                    CobraBayView cobraBayView = launcherWindow.GetCobraBayView();

                    Debug.Assert( cobraBayView != null );
                    if ( cobraBayView != null )
                    {

                        cobraBayView.StartSelectedProject();
                    }
                }
            } );
        }

        /// <summary>
        /// Our ProductUserCtrl
        /// </summary>
        private ProductUserCtrl m_productUserCtrl = null;
    }
}
