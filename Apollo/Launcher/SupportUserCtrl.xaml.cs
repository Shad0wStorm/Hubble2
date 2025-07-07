//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! SupportUserCtrl 
//
//! Author:     Alan MacAree
//! Created:    11 Nov 2022
//----------------------------------------------------------------------

using System.Windows.Controls;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for SupportUserCtrl.xaml
    /// </summary>
    public partial class SupportUserCtrl : UserControl
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public SupportUserCtrl()
        {
            InitializeComponent();
            PART_LinkPackageUserCtrl.Link = LocalResources.Properties.Resources.SupportLink;
        }
    }
}
