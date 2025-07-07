using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using ClientSupport;

namespace SharedControls
{
    /// <summary>
    /// Interaction logic for LoginDialog.xaml
    /// </summary>
    /// 


    public partial class LoginDialog : UserControl
    {
        public event EventHandler SubmitUserDetails;

        public LoginDialog()
        {
            InitializeComponent();
            Remember.IsChecked = true;
        }

        public void SetUser(UserDetails details)
        {
            UserName.Text = details.EmailAddress;
            EFF.Password = details.Password;
            TwoFactor.Text = null; // If required, we always require that the user enter the verification code.
        }

        public void SetStatus(String status)
        {
            Status.Text = status;
        }

        public void SetRequiresTwoFactor(bool requires)
        {
            Visibility unpwvis = requires ? Visibility.Collapsed : Visibility.Visible;
            Visibility tfvis = requires ? Visibility.Visible : Visibility.Collapsed;
            UserName.Visibility = unpwvis;
            UserNameLabel.Visibility = unpwvis;
            EFF.Visibility = unpwvis;
            EFFLabel.Visibility = unpwvis;
            Remember.Visibility = unpwvis;
            TwoFactor.Visibility = tfvis;
            TwoFactorLabel.Visibility = tfvis;
        }

        public void SetFocus()
        {
            if (UserName.Visibility == Visibility.Visible)
            {
                UserName.SelectAll();
                UserName.Focus();
            }
            else
            {
                if (EFF.Visibility == Visibility.Visible)
                {
                    EFF.SelectAll();
                    EFF.Focus();
                }
                else
                {
                    if (TwoFactor.Visibility == Visibility.Visible)
                    {
                        TwoFactor.SelectAll();
                        TwoFactor.Focus();
                    }
                }
            }
        }

        public void GetUser(UserDetails details)
        {
            details.EmailAddress = UserName.Text;
            details.Password = EFF.Password;
            details.TwoFactor = TwoFactor.Text;
        }

        public bool RememberMe { get { return Remember.IsChecked == true; } }

        private void SubmitDetails(object sender, RoutedEventArgs e)
        {
            if (SubmitUserDetails != null)
            {
                SubmitUserDetails(this, new EventArgs());
            }
        }

        private void ReceiveFocus(object sender, RoutedEventArgs e)
        {
            TextBox focussed = sender as TextBox;
            if (focussed != null)
            {
                focussed.SelectAll();
            }
            PasswordBox pwfocus = sender as PasswordBox;
            if (pwfocus != null)
            {
                pwfocus.SelectAll();
            }
        }
    }
}
