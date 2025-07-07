//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! NoWindowTitleStyle, handles the button events from NoWindowTitleStyle
//
//! Author:     Alan MacAree
//! Created:    24 Oct 2022
//----------------------------------------------------------------------

using System.Windows;

namespace Launcher.Styles
{
    public partial class NoWindowTitleStyleClass : ResourceDictionary
    {
        /// <summary>
        /// Called when the close button is clicked
        /// </summary>
        /// <param name="_sender"></param>
        /// <param name="_e"></param>
        private void OnClose( object _sender, RoutedEventArgs _e )
        {
            FrameworkElement frameworkElement = _sender as FrameworkElement;
            if ( frameworkElement != null )
            {
                Window owner = frameworkElement.TemplatedParent as Window;
                if ( owner != null )
                {
                    owner.Close();
                }
            }
        }

        /// <summary>
        /// Called when the minimise button is clicked
        /// </summary>
        /// <param name="_sender"></param>
        /// <param name="_e"></param>
        private void OnMinimise( object _sender, RoutedEventArgs _e )
        {
            FrameworkElement frameworkElement = _sender as FrameworkElement;
            if ( frameworkElement != null )
            {
                Window owner = frameworkElement.TemplatedParent as Window;
                if ( owner != null )
                {
                    owner.WindowState = WindowState.Minimized;
                }
            }
        }
    }
}
