//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! ButtonActionSettings, handles the text and action of clicking on
// Settings in the UI
//
//! Author:     Alan MacAree
//! Created:    13 Sept 2022
//----------------------------------------------------------------------

using System.Diagnostics;
using System.Windows.Navigation;

namespace Launcher
{
    /// <summary>
    /// Handles the text and action of installing a game
    /// </summary>
    internal class ButtonActionSettings : ButtonAction
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cobraBayView">The CobraBayView object</param>
        /// <param name="launcherWindow">The LauncherWindow object</param>
        public ButtonActionSettings( LauncherWindow _launcherWindow, ProductUserCtrl _productUserCtrl )
            : base( _launcherWindow )
        {
            m_productUserCtrl = _productUserCtrl;
        }

        /// <summary>
        /// The derived class should return the button text for the specific action.
        /// </summary>
        /// <returns>The string to display on a button for this action</returns>
        public override string ButtonText()
        {
            return LocalResources.Properties.Resources.BTNT_LauncherSettings;
        }

        /// <summary>
        /// The derived class should perform the specific action.
        /// </summary>
        /// <returns>True if the action started without error</returns>
        public override bool PerformAction( object _sender )
        {
            bool wasActionPerformedOkay = false;

            LauncherWindow launcherWindow = GetLauncherWindow();

            NavigationService ns = NavigationService.GetNavigationService( m_productUserCtrl );
            wasActionPerformedOkay = ns.Navigate( new SettingsPage( launcherWindow ) );

            return wasActionPerformedOkay;
        }

        /// <summary>
        /// Our ProductUserCtrl
        /// </summary>
        private ProductUserCtrl m_productUserCtrl = null;
    }
}
