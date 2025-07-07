//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! DownloadInfoUserCtrl, provides information about
//                        the current download.
//
//! Author:     Alan MacAree
//! Created:    07 Nov 2022
//----------------------------------------------------------------------

using CBViewModel;
using System.Windows.Controls;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for DownloadInfoUserCtrl.xaml
    /// </summary>
    public partial class DownloadInfoUserCtrl : UserControl
    {
        /// <summary>
        /// Our CobraBayView
        /// </summary>
        public CobraBayView TheCobraBayView { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public DownloadInfoUserCtrl()
        {
            InitializeComponent();
        }
    }
}
