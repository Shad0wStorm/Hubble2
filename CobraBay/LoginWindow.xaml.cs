using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Threading.Tasks;
using CBViewModel;
using ClientSupport;
using System.Windows.Interop;

namespace CobraBay
{
    public partial class LoginWindow : Window, ILoginDisplay, INotifyPropertyChanged
    {
        private readonly LoginView _view;

        private bool _statusAsError;
        public bool StatusAsError
        {
            get => _statusAsError;
            set
            {
                if (_statusAsError != value)
                {
                    _statusAsError = value;
                    RaisePropertyChanged(nameof(StatusAsError));
                    if (value)
                    {
                        Dispatcher.InvokeAsync(async () =>
                        {
                            await Task.Delay(5000);
                            SetStatus(string.Empty);
                            StatusAsError = false;
                        });
                    }
                }
            }
        }

        public LoginWindow(FORCManager manager, bool forceSoftwareRendering)
        {
            if (forceSoftwareRendering)
            {
                Loaded += OnLoaded_ForceSoftwareRendering;
            }
            InitializeComponent();

            _view = new LoginView(manager, this);
            Activate();
            _view.Update();

            RememberCheck.IsChecked = _view.RememberUsername;
            RememberPasswordCheck.IsChecked = _view.RememberPassword;

            RememberCheck.Checked += CheckBoxChange;
            RememberCheck.Unchecked += CheckBoxChange;
            RememberPasswordCheck.Checked += CheckBoxChange;
            RememberPasswordCheck.Unchecked += CheckBoxChange;

            CheckBoxChange(null, null);
        }

        private void OnLoaded_ForceSoftwareRendering(object sender, EventArgs e)
        {
            if (PresentationSource.FromVisual(this) is HwndSource hwndSource)
            {
                hwndSource.CompositionTarget.RenderMode = RenderMode.SoftwareOnly;
            }
        }

        public void SetDisplayMode(LoginView.DisplayMode mode)
        {
            StatusLabel.DataContext = this;
            StatusBorder.DataContext = this;

            switch (mode)
            {
                case LoginView.DisplayMode.UserPass:
                    EnableUserNamePassword();
                    StatusAsError = true;
                    break;
                case LoginView.DisplayMode.Verification:
                    EnableVerification();
                    StatusAsError = false;
                    break;
            }
        }

        public void SetStatus(string status) => StatusLabel.Text = status;

        private void EnableUserNamePassword()
        {
            VerificationPanel.Visibility = Visibility.Hidden;
            LoginPanel.Visibility = Visibility.Visible;

            UserNameEdit.Text = _view.EmailAddress;
            PasswordEdit.Password = _view.Password;

            RememberPasswordCheck.Visibility = RememberCheck.IsChecked == true ? Visibility.Visible : Visibility.Hidden;

            if (!string.IsNullOrEmpty(UserNameEdit.Text) && string.IsNullOrEmpty(PasswordEdit.Password))
            {
                PasswordEdit.Focus();
            }
            else
            {
                UserNameEdit.Focus();
            }
        }

        private void EnableVerification()
        {
            VerificationPanel.Visibility = Visibility.Visible;
            LoginPanel.Visibility = Visibility.Hidden;

            VerificationEdit.Clear();
            VerificationEdit.Focus();
        }

        private void OnClose(object sender, RoutedEventArgs e) => Close();

        private void AbortLogin(object sender, CancelEventArgs e)
        {
            if (!_view.CancelLogin())
            {
                e.Cancel = true;
            }
        }

        private void CancelLogin(object sender, RoutedEventArgs e) => Close();

        public bool CheckCancel()
        {
            var result = MessageBox.Show(LocalResources.Properties.Resources.LW_CancelWarning,
                                          LocalResources.Properties.Resources.LW_CancelWarningTitle,
                                          MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            return result != MessageBoxResult.OK;
        }

        private void SubmitLogin(object sender, RoutedEventArgs e)
        {
            if (_view.SubmitLogin(UserNameEdit.Text, PasswordEdit.Password, VerificationEdit.Text,
                                   RememberCheck.IsChecked == true, RememberPasswordCheck.IsChecked == true))
            {
                DialogResult = true;
            }
        }

        private void OnForgotPasswordClick(object sender, RoutedEventArgs e) => _view.PasswordForgotten();

        private void OnRegisterClick(object sender, RoutedEventArgs e)
        {
            if (_view.OpenRegisterLink())
            {
                Close();
            }
        }

        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void CheckBoxChange(object sender, RoutedEventArgs e)
        {
            if (_view != null)
            {
                _view.RememberUsername = RememberCheck.IsChecked == true;
                _view.RememberPassword = RememberPasswordCheck.IsChecked == true;
                _view.UpdateDetails(UserNameEdit.Text, PasswordEdit.Password);
            }
        }

        private void LimitWindow(object sender, MouseButtonEventArgs e)
        {
            double leftLimit = System.Windows.SystemParameters.VirtualScreenLeft;
            double topLimit = System.Windows.SystemParameters.VirtualScreenTop;
            double rightLimit = leftLimit + System.Windows.SystemParameters.VirtualScreenWidth;
            double bottomLimit = topLimit + System.Windows.SystemParameters.VirtualScreenHeight;

            Left = Math.Max(leftLimit, Math.Min(Left, rightLimit - ActualWidth));
            Top = Math.Max(topLimit, Math.Min(Top, bottomLimit - ActualHeight));
        }

        private void UpdateWindowSize(object sender, SizeChangedEventArgs e)
        {
            MinHeight = Math.Max(MinHeight, ActualHeight);
            MinWidth = Math.Max(MinWidth, ActualWidth);
        }

        private void KeyPressed(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    if (UserNameEdit.IsFocused)
                    {
                        e.Handled = true;
                        PasswordEdit.Focus();
                    }
                    else
                    {
                        e.Handled = true;
                        SubmitLogin(null, null);
                    }
                    break;
                case Key.Escape:
                    e.Handled = true;
                    CancelLogin(null, null);
                    break;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string property) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
    }
}
