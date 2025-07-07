//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! ButtonActionManage, handles the text and action of playing a game
//
//! Author:     Alan MacAree
//! Created:    27 Jan 2023
//----------------------------------------------------------------------

using CBViewModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Launcher
{
    /// <summary>
    /// Handles the text and action of getting to the Manage buttons
    /// </summary>
    internal class ButtonActionManage : ButtonAction
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="launcherWindow">The LauncherWindow object</param>
        /// <param name="_productUserCtrl">The ProductUserCtrl object</param>
        public ButtonActionManage( LauncherWindow launcherWindow, ProductUserCtrl _productUserCtrl )
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
            return LocalResources.Properties.Resources.MenuUserManage;
        }

        /// <summary>
        /// The derived class should perform the specific action.
        /// </summary>
        /// <returns>True if the action was ran without error</returns>
        public override bool PerformAction( object _sender )
        {
            if ( m_productUserCtrl != null )
            {
                m_productUserCtrl.DisplayManageButtons();
            }
            return true;
        }

        /// <summary>
        /// Our ProductUserCtrl
        /// </summary>
        private ProductUserCtrl m_productUserCtrl = null;
    }
}
