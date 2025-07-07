//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//! ErrorMessageUserCtrl UserControl
//
//! Author:     Alan MacAree
//! Created:    02 Sep 2022
//----------------------------------------------------------------------

using System.Windows;
using System.Windows.Controls;

namespace FDUserControls
{
    /// <summary>
    /// Interaction logic for ErrorMessageUserCtrl.xaml
    /// </summary>
    public partial class ErrorMessageUserCtrl : UserControl
    {
        /// <summary>
        /// Exposes the TextBox Text as TextBoxText, use this to set the text
        /// within the control
        /// </summary>
        public static readonly DependencyProperty ErrorMessageProperty =
            DependencyProperty.Register( nameof(ErrorMessage), typeof(string), typeof(ErrorMessageUserCtrl));
        public string ErrorMessage
        {
            get { return (string)GetValue( ErrorMessageProperty ); }
            set { SetValue( ErrorMessageProperty, value ); }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public ErrorMessageUserCtrl()
        {
            InitializeComponent();
        }
    }
}
