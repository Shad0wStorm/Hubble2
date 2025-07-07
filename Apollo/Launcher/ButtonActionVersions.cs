//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! ButtonActionVersion, handles the text and action of clicking on
// Versions in the UI
//
//! Author:     Alan MacAree
//! Created:    13 Sept 2022
//----------------------------------------------------------------------

using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Launcher
{
    /// <summary>
    /// Handles the text and action of installing a game
    /// </summary>
    internal class ButtonActionVersion : ButtonAction
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cobraBayView">The CobraBayView object</param>
        /// <param name="launcherWindow">The LauncherWindow object</param>
        public ButtonActionVersion( LauncherWindow _launcherWindow,
                                    FrontPage _frontPage,
                                    ProductUserCtrl _productUserCtrl,
                                    Page _previousPage )
            : base( _launcherWindow )
        {
            Debug.Assert( _productUserCtrl != null );
            Debug.Assert( _frontPage != null );
            Debug.Assert( _previousPage != null );

            m_productUserCtrl = _productUserCtrl;
            m_frontPage = _frontPage;
            m_previousPage = _previousPage;
        }

        /// <summary>
        /// The derived class should return the button text for the specific action.
        /// </summary>
        /// <returns>The string to display on a button for this action</returns>
        public override string ButtonText()
        {
            return LocalResources.Properties.Resources.BTNT_LauncherVersions;
        }

        /// <summary>
        /// The derived class should perform the specific action.
        /// </summary>
        /// <returns>True if the action started without error</returns>
        public override bool PerformAction( object _sender )
        {
            bool wasActionPerformedOkay = false;

            LauncherWindow launcherWindow = GetLauncherWindow();

            Debug.Assert( launcherWindow != null );
            Debug.Assert( m_productUserCtrl != null );

            if ( launcherWindow != null && 
                 m_productUserCtrl  != null &&
                 m_frontPage != null )
            {
                NavigationService ns = NavigationService.GetNavigationService( m_productUserCtrl );
                wasActionPerformedOkay = ns.Navigate( new VersionsPage( launcherWindow, m_frontPage, m_previousPage ) );
            }

            return wasActionPerformedOkay;
        }

        /// <summary>
        /// Our ProductUserCtrl
        /// </summary>
        private ProductUserCtrl m_productUserCtrl = null;

        /// <summary>
        /// The page we return to one the action has been performed
        /// </summary>
        private Page m_previousPage = null;

        /// <summary>
        /// Our FrontPage
        /// </summary>
        private FrontPage m_frontPage = null;
    }
}
