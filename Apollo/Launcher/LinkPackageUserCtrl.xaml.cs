//----------------------------------------------------------------------
// Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
// LinkPackageUserCtrl UserControl, this is used to display
// a title, instructions and a button. This is intended to
// display link information that has a title, instructions or
// a description and a button to link the user to a web site.
//
// Author:     Alan MacAree
// Created:    11 Nov 2022
//----------------------------------------------------------------------

using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for LinkPackageUserCtrl.xaml
    /// </summary>
    public partial class LinkPackageUserCtrl : UserControl
    {
        /// <summary>
        /// Exposes the Title
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(LinkPackageUserCtrl));
        public string Title
        {
            get { return (string)GetValue( TitleProperty ); }
            set { SetValue( TitleProperty, value ); }
        }

        /// <summary>
        /// Exposes the instructions
        /// </summary>
        public static readonly DependencyProperty InstructionsProperty =
            DependencyProperty.Register("Instructions", typeof(string), typeof(LinkPackageUserCtrl));
        public string Instructions
        {
            get { return (string)GetValue( InstructionsProperty ); }
            set { SetValue( InstructionsProperty, value ); }
        }

        /// <summary>
        /// Exposes the Button Text
        /// </summary>
        public static readonly DependencyProperty ButtonTextProperty =
            DependencyProperty.Register("ButtonText", typeof(string), typeof(LinkPackageUserCtrl));
        public string ButtonText
        {
            get { return (string)GetValue( ButtonTextProperty ); }
            set { SetValue( ButtonTextProperty, value ); }
        }

        /// <summary>
        /// Exposes the Link
        /// </summary>
        public static readonly DependencyProperty LinkProperty =
            DependencyProperty.Register("Link", typeof(string), typeof(LinkPackageUserCtrl));
        public string Link
        {
            get { return (string)GetValue( LinkProperty ); }
            set { SetValue( LinkProperty, value ); }
        }

        /// <summary>
        /// Default contructor
        /// </summary>
        public LinkPackageUserCtrl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// When user clicks on the button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClick( object sender, RoutedEventArgs e )
        {
            if ( !string.IsNullOrWhiteSpace( Link ) )
            {
                Process.Start( new ProcessStartInfo( Link ) );
            }
            e.Handled = true;
        }

        /// <summary>
        /// Shows or hides (by Collapsing) the control.
        /// </summary>
        /// <param name="_showControl">If true then the control is Visible, else it is Collapsed</param>
        public void Show( bool _showControl = true )
        {
            if ( _showControl )
            {
                this.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                this.Visibility = System.Windows.Visibility.Collapsed;
            }
        }
    }
}

