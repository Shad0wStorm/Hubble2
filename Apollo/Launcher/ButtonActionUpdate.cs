//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! ButtonActionUpdate, handles the text and action of updating a game
//
//! Author:     Alan MacAree
//! Created:    13 Sept 2022
//----------------------------------------------------------------------

using CBViewModel;
using System.Diagnostics;

namespace Launcher
{
    /// <summary>
    /// Handles the text and action of installing a game
    /// </summary>
    internal class ButtonActionUpdate : ButtonActionProgressBar
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cobraBayView">The CobraBayView object</param>
        /// <param name="launcherWindow">The LauncherWindow object</param>
        public ButtonActionUpdate( LauncherWindow launcherWindow, ProductUserCtrl _productUserCtrl )
            : base( launcherWindow )
        {
            m_productUserCtrl = _productUserCtrl;
        }

        /// <summary>
        /// The derived class should return the button text for the specific action.
        /// </summary>
        /// <returns>The string to display on a button for this action</returns>
        public override string ButtonText()
        {
            return LocalResources.Properties.Resources.BTNT_LauncherUpdate;
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
            if ( launcherWindow != null )
            {
                CobraBayView cobraBayView = launcherWindow.GetCobraBayView();

                Debug.Assert( cobraBayView != null );
                if ( cobraBayView != null )
                {
                    cobraBayView.Upgrade();
                    wasActionPerformedOkay = true;
                }
            }

            return wasActionPerformedOkay;
        }

        /// <summary>
        /// Our ProductUserCtrl
        /// </summary>
        private ProductUserCtrl m_productUserCtrl = null;
    }
}
