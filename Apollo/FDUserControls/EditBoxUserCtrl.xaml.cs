//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! EditBoxUserCtrl UserControl
//
//! Author:     Alan MacAree
//! Created:    02 Sep 2022
//----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FDUserControls
{
    /// <summary>
    /// Interaction logic for FDTextBoxUserCtrl.xaml
    /// This provides a label and a text field (can be a hidden password) within 
    /// a rounded cornered box. The password field has an expose password facility.
    /// </summary>
    public partial class EditBoxUserCtrl : UserControl
    {
        /// <summary>
        /// Exposes the Label Content as LabelContent, use this to set 
        /// the label within the control
        /// </summary>
        public static readonly DependencyProperty LabelContentProperty =
            DependencyProperty.Register( nameof(LabelContent), typeof(object), typeof(EditBoxUserCtrl));
        public object LabelContent
        {
            get { return (object)GetValue( LabelContentProperty ); }
            set { SetValue( LabelContentProperty, value ); }
        }

        /// <summary>
        /// Exposes the TextBox Text as TextBoxText, use this to set the editable text
        /// within the control (not for passwords)
        /// </summary>
        public static readonly DependencyProperty TextBoxProperty =
            DependencyProperty.Register( nameof(TextBoxText), typeof(string), typeof(EditBoxUserCtrl));
        public string TextBoxText
        {
            get { return (string)GetValue( TextBoxProperty ); }
            set { SetValue( TextBoxProperty, value ); }
        }

        /// <summary>
        /// Exposes a WaterMark, this is displayed within the visible text field when that
        /// text field is empty. Used if this is a Text or Password field.
        /// </summary>
        public static readonly DependencyProperty WaterMarkProperty =
            DependencyProperty.Register( nameof(WaterMark), typeof(string), typeof(EditBoxUserCtrl));
        public string WaterMark
        {
            get { return (string)GetValue( WaterMarkProperty ); }
            set { SetValue( WaterMarkProperty, value ); }
        }

        /// <summary>
        /// Exposes the WaterMark Color 
        /// </summary>
        public static readonly DependencyProperty WaterMarkColorProperty =
            DependencyProperty.Register( nameof(WaterMarkColor), typeof(SolidColorBrush), typeof(EditBoxUserCtrl));
        public SolidColorBrush WaterMarkColor
        {
            get { return (SolidColorBrush)GetValue( WaterMarkColorProperty ); }
            set { SetValue( WaterMarkColorProperty, value ); }
        }

        /// <summary>
        /// Expose the IsPasswordField as IsPasswordField, use this to set this control
        /// as a password control, the password text will be hidden.
        /// </summary>
        public static readonly DependencyProperty IsPasswordFieldProperty =
            DependencyProperty.Register( nameof(IsPasswordField), typeof(bool), typeof(EditBoxUserCtrl));
        public bool IsPasswordField
        {
            get { return (bool)GetValue( IsPasswordFieldProperty ); }
            set { SetValue( IsPasswordFieldProperty, value ); }
        }

        /// <summary>
        /// Allows this control to be set as the default control
        /// </summary>
        public static readonly DependencyProperty IsDefaultProperty =
            DependencyProperty.Register( nameof(IsDefault), typeof(bool), typeof(EditBoxUserCtrl));
        public bool IsDefault
        {
            get { return (bool)GetValue( IsDefaultProperty ); }
            set { SetValue( IsDefaultProperty, value ); }
        }

        /// <summary>
        /// Allows this control to allow (default = true) or disallow spaces
        /// </summary>
        public static readonly DependencyProperty AllowSpacesProperty =
            DependencyProperty.Register( nameof( AllowSpaces ), typeof( bool ), typeof( EditBoxUserCtrl ) );
        public bool AllowSpaces
        {
            get { return (bool)GetValue( AllowSpacesProperty ); }
            set { SetValue( AllowSpacesProperty, value ); }
        }

        /// <summary>
        /// Allows the max length property of the text or password to be set, defaults to no limit (0)
        /// </summary>
        public static readonly DependencyProperty MaxLengthProperty =
            DependencyProperty.Register( nameof( MaxLength ), typeof( int ), typeof( EditBoxUserCtrl ) );
        public int MaxLength
        {
            get { return (int)GetValue( MaxLengthProperty ); }
            set { SetValue( MaxLengthProperty, value ); }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public EditBoxUserCtrl()
        {
            InitializeComponent();

            // Setup the defaults
            IsPasswordField = false;
            AllowSpaces = true;
            MaxLength = 0;
        }

        /// <summary>
        /// Allows focus to be set on the internal text controls
        /// </summary>
        /// <returns>true if the focus was set okay</returns>
        public new bool Focus()
        {
            bool didWeSetTheFocus = false;

            if ( IsPasswordField )
            {
                // If this is a password field, we could be dealing with a
                // password that is hidden or visible. 
                if ( !m_passwordRevealed )
                {
                    // Password is hidden
                    didWeSetTheFocus = PART_PasswordBox.Focus();
                }
                else
                {
                    // Password is visible
                    didWeSetTheFocus = PART_TextBox.Focus();
                }
            }
            else
            {
                // This is a normal text control
                didWeSetTheFocus = PART_TextBox.Focus();
            }

            return didWeSetTheFocus;
        }

        /// <summary>
        /// Called when the user control is loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnLoaded( object sender, RoutedEventArgs e )
        {
            // Add our Pasting Handlers
            DataObject.AddPastingHandler( PART_PasswordBox, OnPaste );
            DataObject.AddPastingHandler( PART_TextBox, OnPaste );

            // Do we need to set focus
            if ( IsDefault )
            {
                _ = Focus();
            }
        }

        /// <summary>
        /// Called when the user control is unloaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnUnloaded( object sender, RoutedEventArgs e )
        {
            // Remove our Pasting Handlers
            DataObject.RemovePastingHandler( PART_PasswordBox, OnPaste );
            DataObject.RemovePastingHandler( PART_TextBox, OnPaste );

            // Clear out our list of controls
            m_editBoxUserCtrlList?.Clear();
            m_editBoxUserCtrlList = null;

        }

        /// <summary>
        /// Joins (keeps) and aligns all of the pass controls in order to align the TextBoxes
        /// and PasswordBox within the ctrl, including this one. Allows a bunch of these controls
        /// to be aligned no matter the length of the labels within each control. "this" object is 
        /// included in the list automatically and does not need to be passed within the list.
        /// </summary>
        /// <param name="_EditBoxUserCtrlList">The list of controls that should be aligned with this one</param>
        public void JoinAndAlignControls( List<EditBoxUserCtrl> _editBoxUserCtrlList )
        {
            m_editBoxUserCtrlList = _editBoxUserCtrlList;

            if ( m_editBoxUserCtrlList != null )
            {
                // Just in case...
                // Remove "this" from the list, this then allows memory to be recovered,
                // otherwise we could end up with a circular reference and these objects
                // will not be garbage collected. We keep iterating even when we found
                // "this" in case someone added "this" more than once.
                for ( int idx = 0; idx < m_editBoxUserCtrlList.Count; idx++ )
                {
                    EditBoxUserCtrl ebcu = m_editBoxUserCtrlList[idx];
                    if ( ebcu != null )
                    {
                        if ( ebcu == this )
                        {
                            m_editBoxUserCtrlList.RemoveAt( idx );
                            idx--;
                        }
                    }
                    else
                    {
                        // We found a null EditBoxUserCtrl object passed to this method,
                        // this is not allowed, remove it.
                        m_editBoxUserCtrlList.RemoveAt( idx );
                        idx--;
                        Debug.Assert( false );
                    }
                }

                AlignControls();
            }
        }

        /// <summary>
        /// Aligns all of the controls so that the start of the Label, Space and Editboxes start at the same location.
        /// </summary>
        /// <param name="_EditBoxUserCtrlList">The list of controls that should be aligned with this one</param>
        public void AlignControls()
        {
            // Approach:
            // Find the longest label width by iterating over all the controls.
            // Then set the PART_SpaceColumn width to pad out all of the
            // shorter controls to match the longest control.

            // Note that "this" is not in the list we are iterating over, therefore
            // we start by using "this" width.
            double maxWidth = this.PART_ViewLabel.ActualWidth; 

            // Iterate over our list of EditBoxUserCtrls and find the max width Label content.
            if ( m_editBoxUserCtrlList != null )
            {
                foreach ( EditBoxUserCtrl ctrl in m_editBoxUserCtrlList )
                {
                    // Just in case we have a null item in the list
                    Debug.Assert( ctrl != null );
                    if ( ctrl != null )
                    {
                        double labelActualWidth = ctrl.PART_ViewLabel.ActualWidth;

                        // Check if we have a larger label and store the new maxWidth
                        if ( labelActualWidth > maxWidth )
                        {
                            maxWidth = labelActualWidth;
                        }
                    }
                }

                // Now we know the max width, set the PART_SpaceColumn.Width
                // on each control in our list to match and align the controls. 
                foreach ( EditBoxUserCtrl aCtrl in m_editBoxUserCtrlList )
                {
                    if ( aCtrl != null )
                    {
                        GridLength spaceWidth = new GridLength( (maxWidth - aCtrl.PART_ViewLabel.ActualWidth), GridUnitType.Pixel );
                        aCtrl.PART_SpaceColumn.Width = spaceWidth;
                    }
                }

                // Now set "this" objects width (as "this" is not in the list)
                GridLength thisSpaceWidth = new GridLength( (maxWidth - this.PART_ViewLabel.ActualWidth), GridUnitType.Pixel );
                this.PART_SpaceColumn.Width = thisSpaceWidth;
            }
        }

        /// <summary>
        /// Called when the control changes size, handles the resizing of the joined ctrls.
        /// We must do this because a ViewBox used with the Label changes things on us.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSizeChanged( object sender, SizeChangedEventArgs e )
        {
            AlignControls();
        }

        /// <summary>
        /// Receives the TextChanged Event and passes it onto whoever is interested.
        /// This is called no matter if we have a password, revealing a password or
        /// if we have a normal TextBox - they all fire a TextChangedEventHandler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTextChanged( object sender, TextChangedEventArgs e )
        {
            // Do we need to display the WaterMark?
            bool displayWaterMark = true;

            // We do not display the WaterMark if we have some
            // text within PART_TextBox.
            if ( PART_TextBox.Text != null )
            {
                if ( PART_TextBox.Text.Length > 0 )
                {
                    displayWaterMark = false;
                }
            }

            // To display or not to display the WaterMark?
            ShowWaterMark( displayWaterMark );

            TextChangedEventHandler?.Invoke( sender, e );
        }

        /// <summary>
        /// Receives the PasswordChanged Event and passes it onto whoever is interested.
        /// This is called no matter if we have a password, revealing a password or
        /// if we have a normal TextBox - they all fire a TextChangedEventHandler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPasswordChanged( object sender, RoutedEventArgs e )
        {
            // Do we need to display the WaterMark?
            bool displayWaterMark = true;

            // We do not display the WaterMark if we have some
            // text within the Password.
            string password = Password();

            if ( password != null )
            {
                if ( password.Length > 0 )
                {
                    displayWaterMark = false;
                }
            }

            // To display or not to display the WaterMark?
            ShowWaterMark( displayWaterMark );

            TextChangedEventHandler?.Invoke( sender, null );
        }


        /// <summary>
        /// Handles a paste event and decides if it is allowed or not depending on
        /// the value of AllowSpaces and the contents of the item being pasted.
        /// This specially stops text containing spaces being pasted into a 
        /// control that does not allow spaces (see AllowSpaces).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPaste( object sender, DataObjectPastingEventArgs e )
        {
            if ( !AllowSpaces )
            {
                if ( e.DataObject.GetDataPresent( typeof( string ) ) )
                {
                    string textBeingPasted = e.DataObject.GetData( typeof( string ) ) as string;
                    if ( TextContainsASpace( textBeingPasted ) )
                    {
                        e.CancelCommand();
                    }
                }
            }
        }

        /// <summary>
        /// Checks to see if the past string contains one or more spaces.
        /// </summary>
        /// <param name="_text">The string to check.</param>
        /// <returns>Returns true if the string contains one or more spaces.</returns>
        private bool TextContainsASpace( string _text )
        {
            bool doesTextContainASpace = false;
            if ( !string.IsNullOrWhiteSpace( _text ) )
            { 
                doesTextContainASpace = ( _text.IndexOf( c_Space ) != -1 );
            }
            return doesTextContainASpace;
        }

        /// <summary>
        /// Receives the PreviewKeyDown on either the TextBox or the PasswordBox.
        /// If spaces are not allowed, then this will stop any spaces being added
        /// to the control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPreviewKeyDown( object sender, System.Windows.Input.KeyEventArgs e )
        {
            if ( !AllowSpaces )
            {
                // This stops the user using the keyboard to enter a space, but does
                // not stop a user pasting something with a space into the control.
                if ( e.Key == System.Windows.Input.Key.Space )
                {
                    e.Handled = true;
                }
            }
            base.OnPreviewKeyDown( e );
        }

        /// <summary>
        /// Shows or hides the WaterMark
        /// </summary>
        /// <param name="_showWaterMark">true will cause the WaterMark to show, false will hide it</param>
        private void ShowWaterMark( bool _showWaterMark )
        {
            // To display or not to display the WaterMark?
            if ( _showWaterMark )
            {
                PART_WaterMark.Foreground = WaterMarkColor;
            }
            else
            {
                PART_WaterMark.Foreground = new SolidColorBrush( Colors.Transparent );
            }
        }

        /// <summary>
        /// Returns the password
        /// </summary>
        /// <returns>The password, can be empty</returns>
        public string Password()
        {
            // The current location of the password can change
            string password = PART_PasswordBox.Password;
            if ( m_passwordRevealed )
            {
                password = PART_TextBox.Text;
            }
            return password;
        }

        /// <summary>
        /// Sets the password
        /// </summary>
        /// <param name="_value">The password to set</param>
        public void Password( string _value )
        {
            PART_PasswordBox.Password = _value;
        }

        /// <summary>
        /// The mouse has been pressed on the reveal password image, we have to
        /// be aware that this is a toggle facility and hence we may be revealing
        /// or hiding a password.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnImagePreviewMouseDown( object sender, System.Windows.Input.MouseButtonEventArgs e )
        {
            m_passwordRevealed = !m_passwordRevealed;

            if ( m_passwordRevealed )
            {
                // We are revealing the password
                PART_TextBox.Text = PART_PasswordBox.Password;
                PART_TextBox.Visibility = Visibility.Visible;
                PART_PasswordBox.Visibility = Visibility.Hidden;
                PART_RevealImage.Source = new BitmapImage( new Uri( c_passwordShownImage, UriKind.Absolute ) );
            }
            else
            {
                // We are hiding the password
                PART_PasswordBox.Password = PART_TextBox.Text;
                PART_PasswordBox.Visibility = Visibility.Visible;
                PART_TextBox.Visibility = Visibility.Hidden;
                PART_RevealImage.Source = new BitmapImage( new Uri( c_passwordHiddenImage, UriKind.Absolute ) );
            }
        }

        /// <summary>
        /// Are we showing the password?
        /// </summary>
        private bool m_passwordRevealed = false;

        /// <summary>
        /// Our text changed handler, use this to be informed when the text
        /// is changed.
        /// </summary>
        public event TextChangedEventHandler TextChangedEventHandler;

        /// <summary>
        /// Used by a managing EditBoxUserCtrl to keep a number of EditBoxUserCtrls
        /// aligned with each other. A managing EditBoxUserCtrl is just any EditBoxUserCtrl
        /// object that is picked to align EditBoxUserCtrls. We keep the list of 
        /// EditBoxUserCtrls so that we can resize the internal space when the size changes, 
        /// this may remain null when not linked to any other controls.
        /// </summary>
        private List<EditBoxUserCtrl> m_editBoxUserCtrlList = null;

        /// <summary>
        /// This is part of a toggle facility to show and hide a password.
        /// Our password shown image (when the user can see the password)
        /// </summary>
        private const string c_passwordShownImage = "pack://application:,,,/FDUserControls;component/Images/PasswordShown.png";

        /// <summary>
        /// This is part of a toggle facility to show and hide a password.
        /// Our password hidden image (when the user can not see the password)
        /// </summary>
        private const string c_passwordHiddenImage = "pack://application:,,,/FDUserControls;component/Images/PasswordHidden.png";

        /// <summary>
        /// Defines a space char, partly used to determine if strings contain spaces.
        /// </summary>
        private const char c_Space = ' ';
    }
}
