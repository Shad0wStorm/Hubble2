//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! FDUtils, a place for collection of useful util type methods.
//
//! Author:     Alan MacAree
//! Created:    27/04/23
//----------------------------------------------------------------------

using System.Diagnostics;

namespace FDUserControls
{
    public class FDUtils
    {
        /// <summary>
        /// Reduces a string to a max length, will also add a ext to the end (so the string will be reduced to 
        /// _maxChars - size of _extWhenReduced. _extWhenReduced is optional.
        /// </summary>
        /// <param name="_string">The string to reduce, should not be null or empty.</param>
        /// <param name="_maxChars">The max number of chars to reduce it to (including the length of the _extWhenReduced parameter if not null or empty).</param>
        /// <param name="_extWhenReduced">The extra chars to add to the end, note the size of this will subtracted from _maxChars, can be null or empty.</param>
        /// <returns>The reduced string with _extWhenReduced appended (if passed) to a max length of _maxChars.</returns>
        public static string ReduceStringToMaxLength( string _string, int _maxChars, string _extWhenReduced )
        {
            string result = _string;

            if ( !string.IsNullOrWhiteSpace( _string ) )
            {
                if ( _string.Length > _maxChars )
                {
                    if ( string.IsNullOrWhiteSpace( _extWhenReduced ) )
                    {
                        // _extWhenReduced is null or empty
                        result = _string.Substring( 0, _maxChars );
                    }
                    else
                    {
                        // _extWhenReduced contains something
                        // We must be able to return the _extWhenReduced
                        if ( _extWhenReduced.Length <= _maxChars )
                        {
                            result = _string.Substring( 0, (_maxChars - _extWhenReduced.Length) ) + _extWhenReduced;
                        }
                        else
                        {
                            // We did not have space to add the _extWhenReduced to the string because 
                            // the _maxChars < _extWhenReduced.Length.
                            Debug.Assert( false );
                        }
                    }
                }
            }

            return result;
        }
    }
}
