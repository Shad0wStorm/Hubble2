//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! FrontierLinksCtrl UserControl
//
//! Author:     Alan MacAree
//! Created:    11 Nov 2022
//----------------------------------------------------------------------

using System.Windows.Controls;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for FrontierLinksCtrl.xaml
    /// </summary>
    public partial class FrontierLinksCtrl : UserControl
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public FrontierLinksCtrl()
        {
            InitializeComponent();

            PART_TutorialsPackageUserCtrl.Link = m_tutorialsURL;
            PART_FSLinkPackageUserCtrl.Link = m_browseStoreURL;
            PART_MyAccountPackageUserCtrl.Link = m_myAccountURL;
            PART_EFLinkPackageUserCtrl.Link = m_goToForumsURL;
        }

        /// <summary>
        /// Tutorials Link
        /// </summary>
        private const string m_tutorialsURL = "https://www.elitedangerous.com/help/";

        /// <summary>
        /// Browse Store
        /// </summary>
        private const string m_browseStoreURL = "https://dlc.elitedangerous.com/";

        /// <summary>
        /// My Account Link
        /// </summary>
        private const string m_myAccountURL = "https://www.frontierstore.net/customer/account/login/";

        /// <summary>
        /// Go To Forums
        /// </summary>
        private const string m_goToForumsURL = "https://forums.frontier.co.uk/categories/elite-dangerous/";
    }
}
