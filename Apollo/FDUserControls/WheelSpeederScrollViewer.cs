//
//! Copyright(c) 2022 Frontier Development Plc
//
//! WheelSpeederScrollViewer, allows some control of the mouse 
//! wheel speed in a ScrollViewer
//
//! Author:     Alan MacAree
//! Created:    28 Apr 2023

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FDUserControls
{
    /// <summary>
    /// Custom ScrollViewer that allows the speed of a mouse wheel that moves the scroll bar to be
    /// controlled.
    /// </summary>
    public class WheelSpeederScrollViewer : ScrollViewer
    {
        /// <summary>
        /// Exposes a WheelSpeedFactor, this is used to increase (> 1) or decrease (<1) speed of the mouse wheel on
        /// a scrollbar.
        /// Defaults to 1.
        /// </summary>
        public static readonly DependencyProperty WheelSpeedFactorProperty =
            DependencyProperty.Register( nameof( WheelSpeedFactor ), typeof( double ), typeof( WheelSpeederScrollViewer ), new FrameworkPropertyMetadata(1d) );
        public double WheelSpeedFactor
        {
            get { return (double)GetValue( WheelSpeedFactorProperty ); }
            set { SetValue( WheelSpeedFactorProperty, value ); }
        }

        /// <summary>
        /// Overrides OnPreviewMouseWheel in order to change the vertical offset
        /// based on the current value of WheelSpeedFactor
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreviewMouseWheel( MouseWheelEventArgs e )
        {
            if ( ScrollInfo is ScrollContentPresenter sI && 
                 ComputedVerticalScrollBarVisibility == Visibility.Visible )
            {
                sI.SetVerticalOffset( VerticalOffset - (e.Delta * WheelSpeedFactor) );
                e.Handled = true;
            }
            base.OnPreviewMouseWheel( e );
        }
    }
}
