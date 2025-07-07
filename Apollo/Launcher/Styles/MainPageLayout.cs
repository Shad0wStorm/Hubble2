//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! MainPageLayout, this defines the layout for main pages within the
//! launcher. This allows use to create a generic Page layout and share
//! it across difference pages.
//
//! Author:     Alan MacAree
//! Created:    17 Nov 2022
//----------------------------------------------------------------------

using System.Windows;
using System.Windows.Controls;

namespace Launcher.Styles
{
    /// <summary>
    /// MainPageLayout provides a standard layout for Pages, this keeps items
    /// such as titles, subtitles, buttons and other sections in the same location. 
    /// This reduces "screen-jump" when the user switches between different pages in 
    /// the UI.
    /// </summary>
    class MainPageLayout : Control
    {
        /// <summary>
        /// Constructor
        /// </summary>
        static MainPageLayout()
        {
            DefaultStyleKeyProperty.OverrideMetadata( typeof( MainPageLayout ),
                                                      new FrameworkPropertyMetadata( typeof( MainPageLayout ) ) );
        }

        /// <summary>
        /// PageTitle, intended to be the main title/header of the page
        /// </summary>
        public static DependencyProperty PageTitleProperty =
            DependencyProperty.Register( "PageTitle", typeof(object),
            typeof(MainPageLayout), new UIPropertyMetadata());

        public object PageTitle
        {
            get { return (object)GetValue( PageTitleProperty ); }
            set { SetValue( PageTitleProperty, value ); }
        }

        /// <summary>
        /// Page Sub title, intended as a sub title of the page
        /// </summary>
        public static DependencyProperty PageSubTitleProperty =
            DependencyProperty.Register( "PageSubTitle", typeof(object),
            typeof(MainPageLayout), new UIPropertyMetadata());

        public object PageSubTitle
        {
            get { return (object)GetValue( PageSubTitleProperty ); }
            set { SetValue( PageSubTitleProperty, value ); }
        }

        /// <summary>
        /// The left side of the page
        /// </summary>
        public static DependencyProperty LeftAreaProperty =
            DependencyProperty.Register( "LeftArea", typeof(object),
            typeof(MainPageLayout), new UIPropertyMetadata());

        public object LeftArea
        {
            get { return (object)GetValue( LeftAreaProperty ); }
            set { SetValue( LeftAreaProperty, value ); }
        }

        /// <summary>
        /// The right side of the page
        /// </summary>
        public static DependencyProperty RightAreaProperty =
            DependencyProperty.Register( "RightArea", typeof(object),
            typeof(MainPageLayout), new UIPropertyMetadata());

        public object RightArea
        {
            get { return (object)GetValue( RightAreaProperty ); }
            set { SetValue( RightAreaProperty, value ); }
        }

        /// <summary>
        /// The main content of the page
        /// </summary>
        public static DependencyProperty ContentProperty =
            DependencyProperty.Register( "Content", typeof(object),
            typeof(MainPageLayout), new UIPropertyMetadata());

        public object Content
        {
            get { return (object)GetValue( ContentProperty ); }
            set { SetValue( ContentProperty, value ); }
        }

    }
}
