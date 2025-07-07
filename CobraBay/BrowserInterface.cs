using System;
using System.Security.Permissions;
using System.Windows;

namespace CobraBay
{
    /// <summary>
    /// Provides an interface to interact with the browser window.
    /// </summary>
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class BrowserInterface
    {
        private readonly CobraBayWindow _window;

        /// <summary>
        /// Initializes a new instance of the <see cref="BrowserInterface"/> class.
        /// </summary>
        /// <param name="window">The window to interact with.</param>
        public BrowserInterface(CobraBayWindow window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window), "Window cannot be null.");
        }

        /// <summary>
        /// Gets or sets a flag indicating whether internal navigation is in progress.
        /// </summary>
        public bool InternalNavigation { get; set; } = false;

        /// <summary>
        /// Enables the browser UI.
        /// </summary>
        public void EnableBrowserUI()
        {
            _window.EnableBrowserUI(true);
        }

        /// <summary>
        /// Closes the browser window.
        /// </summary>
        public void Close()
        {
            _window.Close();
        }

        /// <summary>
        /// Minimizes the browser window.
        /// </summary>
        public void Minimize()
        {
            _window.WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// Shows the options for the browser window.
        /// </summary>
        public void ShowOptions()
        {
            _window.ShowOptions();
        }

        /// <summary>
        /// Hides the release notes link in the browser window.
        /// </summary>
        public void HideReleaseNotesLink()
        {
            _window.HideReleaseNotesLink();
        }
    }
}
