//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! WaitUserCtrl UserControl
//
//! An animated spinner with optional sub text
//
//! Author:     Alan MacAree
//! Created:    14 Sep 2022
//----------------------------------------------------------------------

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace FDUserControls
{
    /// <summary>
    /// Interaction logic for WaitUserCtrl.xaml
    /// This control provides a circle of rotating blobs
    /// with optional sub text and is intended as a wait spinner.
    /// This control has its own ViewBox and hence will auto
    /// resize both the spinner and sub text.
    /// </summary>
    public partial class WaitUserCtrl : UserControl
    {
        /// <summary>
        /// Default number of rotations per sec
        /// </summary>
        private const double c_defaultRotationsPerSec = 1d;

        /// <summary>
        /// Number of blob positions, this is used to determine
        /// the speed of rotation along with the positions of
        /// each blob.
        /// </summary>
        private const int c_noOfBlobPositions = 12;

        /// <summary>
        /// The degrees we spin our spinner on each step.
        /// This is based on 360 degrees / the number of blob positions
        /// </summary>
        private const double c_spinByDegree = 360d/c_noOfBlobPositions;

        /// <summary>
        /// The number of blobs we have.
        /// </summary>
        private const int c_numberOfBlobs = c_noOfBlobPositions-1;

        /// <summary>
        /// The number of blobs that can fit into half a circle
        /// </summary>
        private const double c_blobsPerHalfCircle = c_noOfBlobPositions/2;

        /// <summary>
        /// The default size of the Blobs Outer Thickness. This is used
        /// so that a different colour may be used on the outer rim of
        /// the blobs (makes it look nice).
        /// </summary>
        private const double c_defaultBlobsOuterThinkness = 0.2;

        /// <summary>
        /// Our timer used to spin the spinner
        /// </summary>
        private DispatcherTimer m_timer;

        /// <summary>
        /// The circle diameter.
        /// This is also used within the XAML.
        /// </summary>
        public double CircleDiameter => 100;

        /// <summary>
        /// BlobsDiameters are related to the diameter of the circle.
        /// This is also used within the XAML.
        /// </summary>
        public double BlobsDiameter => CircleDiameter / 6.5;

        /// <summary>
        /// Exposes the Blobs background colour as "BlobsBackground"
        /// </summary>
        public static readonly DependencyProperty BlobsBackgroundProperty =
            DependencyProperty.Register( nameof(BlobsBackground), typeof(object), typeof(WaitUserCtrl));
        public object BlobsBackground
        {
            get { return (object)GetValue( BlobsBackgroundProperty ); }
            set { SetValue( BlobsBackgroundProperty, value ); }
        }

        /// <summary>
        /// Exposes the speed of rotation as RotationsPerSec. Therefore
        /// 1 means 1 rotation per sec, 0.5 means half a rotation per sec;
        /// and so on.
        /// Rotation speed is only changed when the control becomes visible.
        /// </summary>
        public static readonly DependencyProperty RotationsPerSecProperty =
            DependencyProperty.Register( nameof(RotationsPerSec), typeof(double), typeof(WaitUserCtrl));
        public double RotationsPerSec
        {
            get { return (double)GetValue( RotationsPerSecProperty ); }
            set { SetValue( RotationsPerSecProperty, value ); }
        }

        /// <summary>
        /// Exposes the optional text under the spinner as "LabelContent"
        /// </summary>
        public static readonly DependencyProperty LabelContentProperty =
            DependencyProperty.Register( nameof(LabelContent), typeof(object), typeof(WaitUserCtrl));
        public object LabelContent
        {
            get { return (object)GetValue( LabelContentProperty ); }
            set { SetValue( LabelContentProperty, value ); }
        }

        /// <summary>
        /// Exposes the Label foreground colour as "LabelForeground"
        /// </summary>
        public static readonly DependencyProperty LabelForgroundProperty =
            DependencyProperty.Register( nameof(LabelForeground), typeof(object), typeof(WaitUserCtrl));
        public object LabelForeground
        {
            get { return (object)GetValue( LabelForgroundProperty ); }
            set { SetValue( LabelForgroundProperty, value ); }
        }

        /// <summary>
        /// Exposes the blobs outer ring stroke (outer ring thickness) as "BlobsOuterThickness"
        /// </summary>
        public static readonly DependencyProperty BlobsOuterThicknessProperty =
            DependencyProperty.Register( nameof(BlobsOuterThickness), typeof(double), typeof(WaitUserCtrl));
        public double BlobsOuterThickness
        {
            get { return (double)GetValue( BlobsOuterThicknessProperty ); }
            set { SetValue( BlobsOuterThicknessProperty, value ); }
        }

        /// <summary>
        /// Exposes the Blobs outer colour (outer ring colour) as "BlobsOuterColour"
        /// </summary>
        public static readonly DependencyProperty BlobsOuterColourProperty =
            DependencyProperty.Register( nameof(BlobsOuterColour), typeof(object), typeof(WaitUserCtrl));
        public object BlobsOuterColour
        {
            get { return (object)GetValue( BlobsOuterColourProperty ); }
            set { SetValue( BlobsOuterColourProperty, value ); }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public WaitUserCtrl()
        {
            InitializeComponent();

            // Set the default values
            RotationsPerSec = c_defaultRotationsPerSec;

            BlobsBackground = new SolidColorBrush(  Colors.Black );
            LabelForeground = new SolidColorBrush( Colors.White );

            BlobsOuterThickness = c_defaultBlobsOuterThinkness;
            BlobsOuterColour = new SolidColorBrush( Colors.White );

            // Create our timer that makes things rotate. This starts when 
            // the UserControl is visible.
            m_timer = new DispatcherTimer( DispatcherPriority.Normal, Dispatcher );
            m_timer.Tick += OnRotate;
        }

        /// <summary>
        /// Rotate the spinner, called via a timer
        /// </summary>
        /// <param name="_sender"></param>
        /// <param name="_e"></param>
        private void OnRotate( object snder, EventArgs e )
        {
            double newAngle = SpinnerTransForm.Angle + c_spinByDegree;

            // If we have reached the max, reset to zero
            if ( newAngle >= 360 )
            {
                newAngle -= 360;
            }

            // Rotate our canvas that are blobs are on.
            SpinnerTransForm.Angle = newAngle;
        }

        /// <summary>
        /// Called on user control load
        /// </summary>
        private void UserControl_Loaded( object sender, RoutedEventArgs e )
        {
            // We have a falling opacity, 1 being completely visible.
            double blobOpacity = 1d;
            double blobOpacityReduction = 1d / c_numberOfBlobs;

            // Create our blobs, and reduce the opacity as we go along
            for ( int blobNo = 0; blobNo < c_numberOfBlobs; blobNo++ )
            {
                Ellipse ellipse = new Ellipse();
                ellipse.Opacity = blobOpacity;
                blobOpacity -= blobOpacityReduction;
                PART_Canvas.Children.Add( ellipse );
                SetBlobsStartPosition( ellipse, blobNo );
            }
        }

        /// <summary>
        /// Called on unload
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserControl_Unloaded( object sender, RoutedEventArgs e )
        {
            m_timer.Stop();
            m_timer.Tick -= OnRotate;
            PART_Canvas.Children.Clear();
        }

        /// <summary>
        /// Sets the start position of each blob on the canvas
        /// </summary>
        /// <param name="Blob">The blob control to set</param>
        /// <param name="index">The index of the blob, starting from 0</param>
        private void SetBlobsStartPosition( Ellipse _Blob, int _BlobsIndex )
        {
            double blobsRadius = BlobsDiameter / 2d;
            double circleRadius = CircleDiameter / 2d;
            double centerLeftPosition = circleRadius - blobsRadius;
            double blobsAngle =  Math.PI * (_BlobsIndex / c_blobsPerHalfCircle);

            // Set the top left position of the blob
            _Blob.SetValue( Canvas.LeftProperty, centerLeftPosition + (Math.Sin( blobsAngle ) * centerLeftPosition) );
            _Blob.SetValue( Canvas.TopProperty, centerLeftPosition + (Math.Cos( blobsAngle ) * centerLeftPosition) );
        }

        /// <summary>
        /// Called when we are shown or hidden, makes sure the spin is stopped if not visible
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserControl_IsVisibleChanged( object sender, DependencyPropertyChangedEventArgs e )
        {
            if ( (bool)e.NewValue )
            {
                // If we are visible, then start rotating
                m_timer.Interval = TimeSpan.FromMilliseconds( 1000 / (c_noOfBlobPositions * RotationsPerSec) );
                m_timer.Start();
            }
            else
            {
                // If we are not visible, then stop rotating
                m_timer.Stop();
            }
        }
    }
}
