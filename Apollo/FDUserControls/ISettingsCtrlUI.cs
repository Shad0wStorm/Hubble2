//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! ISettingsCtrlUI, provides UI related interface calls
//
//! Author:     Alan MacAree
//! Created:    22 Aug 2022
//----------------------------------------------------------------------

namespace FDUserControls
{
    /// <summary>
    /// UI classes that wish to react to SettingsCtrl UI events should
    /// implement this interface.
    /// </summary>
    public interface ISettingsCtrlUI
    {
        /// <summary>
        /// Called when the user clicks on Support
        /// </summary>
        void OnSupportBtnClicked();

        /// <summary>
        /// Called when the user clicks on Frontier Links
        /// </summary>
        void OnFrontierLinksBtnClicked();

        /// <summary>
        /// Called when the user clicks on Options
        /// </summary>
        void OnOptionsBtnClicked();

        /// <summary>
        /// Called when a user clicks on the Language
        /// </summary>
        void OnLanguageBtnClicked();

        /// <summary>
        ///  called when the user clicks on Back
        /// </summary>
        void OnBackBtnClicked();

        /// <summary>
        /// Returns the current users name.
        /// </summary>
        /// <returns>Users name or a blank string</returns>
        string GetUsersName();

        /// <summary>
        /// Returns the current users email address
        /// </summary>
        /// <returns>users email address or a blank string</returns>
        string GetUsersEmail();
    }
}
