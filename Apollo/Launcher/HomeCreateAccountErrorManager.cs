//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! HomeCreateAccountErrorManager, handles the logic to decide which
//! errors are displayed to the user on the HomeCreateAccountPage
//
//! Author:     Alan MacAree
//! Created:    26 April 23
//----------------------------------------------------------------------

using System.Collections.Generic;

namespace Launcher
{
    /// <summary>
    /// HomeCreateAccountErrorManager, this class associates strings with error sources,
    /// such as Email. The error sources have a priority, meaning one error is more
    /// important than other errors that need to be displayed to the user, this is determined
    /// by the value of the error source ErrorSourceAndPriority. Null strings cannot be stored.
    /// </summary>
    public class HomeCreateAccountErrorManager
    {
        public HomeCreateAccountErrorManager()
        {
        }

        /// <summary>
        /// Associates the passed _errorSourceAndPriority with the passed _errorText. This will
        /// overwrite any previous string that has been associated with _errorSourceAndPriority. 
        /// Null strings are not allowed and will be replaced with string.Empty.
        /// </summary>
        /// <param name="_errorSourceAndPriority">The ErrorSourceAndPriority to store</param>
        /// <param name="_errorText">The string to associated with the ErrorSourceAndPriority</param>
        public void StoreError( ErrorSourceAndPriority _errorSourceAndPriority, string _errorText )
        {
            // Do not allow null strings to be stored
            if ( _errorText == null )
            {
                _errorText = string.Empty;
            }

            // Do we need to overwrite or store an association
            if ( DoesContainErrorSourceAndPriority( _errorSourceAndPriority ) )
            {
                m_errorSourceDictionary[_errorSourceAndPriority] = _errorText;
            }
            else
            {
                m_errorSourceDictionary.Add( _errorSourceAndPriority, _errorText );
            }
        }

        /// <summary>
        /// Removes any string associated with _errorSourceAndPriority.
        /// </summary>
        /// <param name="_errorSourceAndPriority">The ErrorSourceAndPriority to remove</param>
        /// <returns>True if the string was removed, or false if no associated string existed.</returns>
        public bool RemoveError( ErrorSourceAndPriority _errorSourceAndPriority )
        {
            return m_errorSourceDictionary.Remove( _errorSourceAndPriority );
        }

        /// <summary>
        /// Returns the highest priority error that is held.
        /// </summary>
        /// <returns>The highest priority ErrorSourceAndPriority held.</returns>
        public ErrorSourceAndPriority GetHighestPriorityErrorSource()
        {
            ErrorSourceAndPriority theHighestPriorityErrorSourceWeHave = ErrorSourceAndPriority.None;

            foreach( KeyValuePair<ErrorSourceAndPriority, string>  keyValuePair in m_errorSourceDictionary )
            {
                if ( keyValuePair.Key > theHighestPriorityErrorSourceWeHave )
                {
                    theHighestPriorityErrorSourceWeHave = keyValuePair.Key;
                }
            }

            return theHighestPriorityErrorSourceWeHave;
        }

        /// <summary>
        /// Returns the string associated with the highest priority error that is held.
        /// </summary>
        /// <returns>The string associated with the highest priority ErrorSourceAndPriority held.</returns>
        public string GetHighestPriorityErrorString()
        {
            string result = string.Empty;

            ErrorSourceAndPriority theHighestPriorityErrorSourceWeHave = GetHighestPriorityErrorSource();
            if ( theHighestPriorityErrorSourceWeHave != ErrorSourceAndPriority.None )
            {
                if ( !m_errorSourceDictionary.TryGetValue( theHighestPriorityErrorSourceWeHave, out result ) )
                {
                    result = string.Empty;
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the string associated with the passed _errorSourceAndPriority.
        /// </summary>
        /// <param name="_errorSourceAndPriority"></param>
        /// <returns>The string associated with the passed _errorSourceAndPriority, may be an empty string</returns>
        public string GetErrorSourceAndPriorityString( ErrorSourceAndPriority _errorSourceAndPriority )
        {
            string result = string.Empty;

            if ( !m_errorSourceDictionary.TryGetValue( _errorSourceAndPriority, out result ) )
            {
                result = string.Empty;
            }

            return result;
        }

        public bool DoesContainErrorSourceAndPriority( ErrorSourceAndPriority _errorSourceAndPriority )
        {
            return m_errorSourceDictionary.ContainsKey( _errorSourceAndPriority );
        }

        /// <summary>
        /// Determines if this object is empty and not storing any errors
        /// </summary>
        /// <returns>Returns true if no errors are stored.</returns>
        public bool IsEmpty()
        {
            return (m_errorSourceDictionary.Count == 0);
        }

        /// <summary>
        /// The error source (where the error can from) in priority order. The
        /// priority order (highest being 2) can be used to determine which
        /// error string to display when we don't know which one to display.
        /// </summary>
        public enum ErrorSourceAndPriority
        {
            None = 0,
            ConfirmPassword,
            Password,
            Email
        }

    
        /// <summary>
        /// Our main container of error strings related to an ErrorSource (what created the error)
        /// </summary>
        private Dictionary<ErrorSourceAndPriority, string> m_errorSourceDictionary = new Dictionary<ErrorSourceAndPriority, string>();
    }
}
