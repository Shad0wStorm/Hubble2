//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------
 
//----------------------------------------------------------------------
//! ConverterBoolToGridLength 
//
//! Author:     Alan MacAree
//! Created:    16 Sep 2022
//
// Converts a bool to a GridLength
// bool == true then the parameter is returned as the GridLength
// bool == false then 0 is returned as the GridLength
//
// This converter is intended to resize GridLengths to zero (hide)
// or use the specified GridLength depending on the bool (value) passed.
//----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FDUserControls
{
    /// <summary>
    /// Allows a bool to be used to specify a GridLength as a specified value or as zero.
    /// If value == true, then the value passed as the parameter is returned as the GridLength.
    /// If value == false, a GridLength of zero is returned.
    /// </summary>
    class ConverterBoolToGridLength : IValueConverter
    {
        /// <summary>
        /// Converts a bool to a GridLength
        /// If true, then the value passed as the parameter is returned as the GridLength
        /// If false, a GridLength of zero is returned.
        /// </summary>
        /// <param name="value">Must be a bool</param>
        /// <param name="targetType"></param>
        /// <param name="parameter">Must be the GridLength</param>
        /// <param name="culture"></param>
        /// <returns>If value==true then the specified GridLength is returned, otherwise zero is returned</returns>
        public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
        {
            // Our default GridLength, this is returned if value == false
            GridLength result = new GridLength( 0d );

            // Convert the value to a bool (true or false)
            bool valueAsBool = false;
            if ( bool.TryParse( value.ToString(), out valueAsBool ) )
            {
                if ( valueAsBool )
                {
                    // Our parameter should be a string containing the GridLength specification.
                    string parameterAsString = parameter as string;
                    if ( parameterAsString != null )
                    {
                        try
                        {
                            // Our default GridLength type.
                            GridUnitType gridUnitType = GridUnitType.Pixel;

                            // Check if we have a * or an auto, note that a * can also have a number.
                            int idxOfStar = parameterAsString.IndexOf( c_starType );
                            int idxOfAuto = parameterAsString.IndexOf( c_autoType );

                            if ( idxOfStar >= 0 && idxOfAuto >= 0 )
                            {
                                // We should not have both a star and an auto in the parameter.
                                // In a release build, this is treated as a star.
                                Debug.Assert( false );
                            }

                            if ( idxOfStar >= 0 )
                            {
                                // We have a star
                                parameterAsString = parameterAsString.Remove( idxOfStar, c_starType.Length );
                                gridUnitType = GridUnitType.Star;
                            }
                            else if ( idxOfAuto >= 0 )
                            {
                                // We have an auto
                                parameterAsString = parameterAsString.Remove( idxOfAuto, c_autoType.Length );
                                gridUnitType = GridUnitType.Auto;
                            }

                            // If we have anything left in our parameter, convert it to a double.
                            // A "1 *" or a "1 auto" is acceptable.
                            double lengthAsDouble = 1d;
                            if ( parameterAsString.Length > 0 )
                            {
                                double.TryParse( parameterAsString, out lengthAsDouble );
                            }

                            // Create the resulting GridLength from what we found.
                            result = new GridLength( lengthAsDouble, gridUnitType );
                        }
                        catch ( Exception )
                        {
                            // Don't do anything, unless we are development.
                            Debug.Assert( false );
                        }
                    }
                    else
                    {
                        // The parameter was not a string, this is not allowed.
                        Debug.Assert( false );
                    }
                }
            }
            else
            {   
                // The value was not a bool, this is not allowed.
                Debug.Assert( false );
            }

            return result;
        }

        /// <summary>
        /// Not impelmented
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Identifies the star type used in GridLengths
        /// </summary>
        private const string c_starType = "*";

        /// <summary>
        /// Identifies the auto type used in GridLengths
        /// </summary>
        private const string c_autoType = "auto";
    }
}
