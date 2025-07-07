//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! LanguageCtrl, this control displays a list of available languages
//  for the user to select from. 
//
//  A delegate is used to provide a callback mechanisum to inform 
//  the client of this control which language code has been selected.
//
//  The list of available langauges is supplied to this control via a
//  LanguageDictionary; which holds language codes and displayable
//  Language strings.
//
//  Users of this object must implement 
//
//! Author:     Alan MacAree
//! Created:    19 Aug 2022
//----------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace FDUserControls
{
    /// Specify our LanguageDictionary
    using LanguageDictionary = Dictionary<string, string>;

    /// Specify our RadioButtonList
    using RadioButtonList = List<RadioButton>;

    /// <summary>
    /// Interaction logic for LanguageUserCtrl.xaml
    /// This handles the display of the available languages
    /// </summary>
    public partial class LanguageUserCtrl : UserControl
    {
        /// <summary>
        /// Callback mechanisum to inform the client of this User Control
        /// which language has been selected.
        /// </summary>
        /// <param name="_languageCode">The selected language code
        /// as specified in the language Dictionary</param>
        public delegate void Callback( string _languageCode );

        /// <summary>
        /// Used to keep the passed delegate
        /// </summary>
        public Callback OnLanguageChange { get; set; }

        /// <summary>
        /// RadioButtonList, used to keep tract of our RadioButtons
        /// </summary>
        private RadioButtonList m_radioButtonList = new RadioButtonList();

        /// <summary>
        /// Default Constructor
        /// </summary>
        public LanguageUserCtrl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Constructor, assigns the callback and LanguageDictionary.
        /// </summary>
        /// <param name="_LanguageDictionary">A LanguageDictionary that holds the language code and language strings</param>
        /// <param name="_selectLanguageKey">The default language key to set as selected</param>
        public LanguageUserCtrl( LanguageDictionary _languageDictionary, string _selectLanguageKey )
        {
            InitializeComponent();
            SetLanguageDictionary( _languageDictionary, _selectLanguageKey );
        }

        /// <summary>
        /// Sets and displays the LanguageDictionary within this object.
        /// </summary>
        /// <param name="_LanguageDictionary">The LanguageDictionary to use</param>
        /// <param name="_selectLanguageKey">The default language key to set as selected</param>
        public void SetLanguageDictionary( LanguageDictionary _languageDictionary, string _selectLanguageKey )
        {
            DisplayLanguages( _languageDictionary, _selectLanguageKey );
        }

        /// <summary>
        /// Displays the languages from the m_LanguageDictionary on the control
        /// </summary>
        /// <param name="_LanguageDictionary">The LanguageDictionary to use</param>
        /// <param name="_selectLanguageKey">The default language key to set as selected</param>
        private void DisplayLanguages( LanguageDictionary _languageDictionary, string _selectLanguageKey )
        {
            // Remove any existing RadioButtons that we have
            m_radioButtonList.Clear();

            // Clear our list of languages and display new ones (if we have any).
            PART_LanguagePanel.Children.Clear();

            if ( _languageDictionary != null )
            {
                // For each KeyValuePair in our Dictionary, create a new RadioButton,
                // add the language name and language code and then add the RadioButton
                // to our UI stack panel.
                foreach ( KeyValuePair<string, string> entry in _languageDictionary )
                {
                    RadioButton radioButton = new RadioButton();
                    radioButton.CommandParameter = entry.Key;
                    radioButton.Content = entry.Value;
                    radioButton.GroupName = c_groupName;

                    // Keep track of our RadioButtons that we add dynamically.
                    m_radioButtonList.Add( radioButton );

                    // Add the button to our LanguagePanel.
                    PART_LanguagePanel.Children.Add( radioButton );

                    // If this key matches the passed _selectLanguageKey
                    // then select it as checked (i.e. the current selected
                    // language).
                    if ( _selectLanguageKey != null )
                    {
                        if ( _selectLanguageKey == entry.Key )
                        {
                            radioButton.IsChecked = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the selected language code (or null).
        /// </summary>
        /// <returns>The selected language code or null if no language is selected</returns>
        private string SelectedLanguageCode()
        {
            string sResult = null;

            // Iterate through our RadioButtons until we find the selected one
            Debug.Assert( m_radioButtonList.Count > 0 );
            for ( int idx = 0; idx < m_radioButtonList.Count && sResult == null; idx++ )
            {
                // It should not be null
                Debug.Assert( m_radioButtonList[idx] != null );
                if ( m_radioButtonList[idx] != null )
                {
                    // Is the RadioButton selected?
                    if ( m_radioButtonList[idx].IsChecked == true )
                    {
                        // We should have a CommandParameter set as a string (the langauge code)
                        Debug.Assert( m_radioButtonList[idx].CommandParameter != null );
                        if ( m_radioButtonList[idx].CommandParameter != null )
                        {
                            string commandParameterAsString = m_radioButtonList[idx].CommandParameter as string;
                            Debug.Assert( commandParameterAsString != null );
                            if ( commandParameterAsString != null )
                            {
                                sResult = commandParameterAsString;
                            }
                        }
                    }
                }
            }

            return sResult;
        }

        /// <summary>
        /// Called when the user clicks on the apply button to 
        /// accept the language that has been selected, the delegate
        /// is used to call the client of this object to inform them
        /// of the selected language code.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnApplyBtnClick( object sender, RoutedEventArgs e )
        {
            // As long as we have a selected language code, inform the client of 
            // this control.
            string theSelectedLanguageCode = SelectedLanguageCode();

            if ( theSelectedLanguageCode != null 
                && OnLanguageChange  != null )
            {
                OnLanguageChange( theSelectedLanguageCode );
            }
        }

        /// <summary>
        /// Group name used to group UI controls together.
        /// </summary>
        private const string c_groupName = "LanguageUserCtrlGroup";
    }
}
