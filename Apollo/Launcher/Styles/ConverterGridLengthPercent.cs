//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! ConverterGridLengthPercent 
//
//! Author:     Alan MacAree
//! Created:    16 Nov 2022
//
// Converts a GridLength into a percentage of itself.
//
//----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Launcher.Styles
{
    /// <summary>
    /// Converts a GridLength into a percentage of itself.
    /// Requires 2 parameters to be passed.
    /// </summary>
    public class ConverterGridLengthPercent : IMultiValueConverter
    {
        /// <summary>
        /// The number of values we expect to be passed to this converter
        /// </summary>
        private const int c_numberOfValuesExpected = 2;

        /// <summary>
        /// The origonal GridLength position in the passed value array
        /// </summary>
        private const int c_lengthValuePosition = 0;

        /// <summary>
        /// The percentage position in the passed value array
        /// </summary>
        private const int c_percentageValuePosition = 1;

        /// <summary>
        /// Converts a GridLength into apercentage of that GridLength
        /// </summary>
        /// <param name="values">An array containing 2 items, 1st item is the length, 2nd is the percentage</param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns>A GridLength</returns>
        public object Convert( object[] values, Type targetType, object parameter, CultureInfo culture )
        {
            double gridLength = 0d;

            if ( values.Length == c_numberOfValuesExpected )
            {
                if ( values[c_lengthValuePosition] != DependencyProperty.UnsetValue &&
                     values[c_percentageValuePosition] != DependencyProperty.UnsetValue )
                {
                    double lengthAsDouble = (double)values[c_lengthValuePosition];
                    int percentageAsDouble = (int)values[c_percentageValuePosition];

                    double calcThickness= lengthAsDouble * ((double)percentageAsDouble / 100d );

                    gridLength = calcThickness;
                }
            }
            else
            {
                Debug.Assert( false );
            }

            return gridLength;
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetTypes"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object[] ConvertBack( object value, Type[] targetTypes, object parameter, CultureInfo culture )
        {
            throw new NotImplementedException();
        }
    }
}
