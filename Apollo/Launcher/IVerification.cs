//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! IVerification, used by the Validation page to call back to the page
//! that started the verification process.
//
//! Author:     Alan MacAree
//! Created:    19 Aug 2022
//----------------------------------------------------------------------

namespace Launcher
{
    public interface IVerification
    { 
        /// <summary>
        /// Must return the title of the page.
        /// This can displayed to the user
        /// </summary>
        /// <returns>The page title</returns>
        string PageTitle();

        /// <summary>
        /// Called to validate a code entered by the user 
        /// and perform any further processing.
        /// Should return true if okay, else false.
        /// </summary>
        /// <param name="_email"></param>
        /// <param name="_password"></param>
        /// <param name="_code"></param>
        /// <returns>true if the code was validated okay</returns>
        bool ValidateCode( string _email,
                           string _password,
                           string _code );

        /// <summary>
        /// The method should request a new code to be sent
        /// to the user.
        /// </summary>
        /// <returns>A developer verification code or null in the case of normal users</returns>
        string ResendCode();
    }
}


