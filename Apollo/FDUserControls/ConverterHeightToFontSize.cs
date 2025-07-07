//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! ConverterHeightToFontSize 
//
//! Converters a Height into a Font Size
//
//! Author:     Alan MacAree
//! Created:    07 Sep 2022
//----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

namespace FDUserControls
{
    /// <summary>
    /// Converts a height into an appropriate fontsize, intended to be used
    /// to determine a font size for a given control height.
    /// </summary>
    public class ConverterHeightToFontSize : IValueConverter
    {
        /// <summary>
        /// Convert a control height to a font size that will
        /// fit the available height. Used in place of Viewbox
        /// when a Viewbox is not appropriate. 
        /// </summary>
        /// <param name="value">Height to convert</param>
        /// <param name="targetType">Not used</param>
        /// <param name="parameter">Not used</param>
        /// <param name="culture">Not used</param>
        /// <returns>The size of the font to fit the height</returns>
        public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
        {
            double fontSize = 0d;

            double height = 0d;
            if ( double.TryParse( value.ToString(), out height ) )
            {
                fontSize = height * c_heightToFontRatio;
            }
            else
            {
                // Cause an assert to fail is we get here, otherwise just ignore it.
                Debug.Assert( false );
            }

            // When a TextBox is created, it can have a zero height, causing a zero FontSize,
            // from the calculation above. this in turn will cause a debug message:
            //  System.Windows.Data Error: 5 : Value produced by BindingExpression ...
            //
            // Using a default font will remove this message, and the FontSize is not used.
            // Also make sure we don't end up with a minus!
            if ( fontSize <= 0 )
            {
                fontSize = c_defaultFontSize;
            }

            return fontSize;
        }

        /// <summary>
        /// We do not implement the ConvertBack
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
        /// This is a magic number which converters a height into a font size.
        /// This number has been generated as the closest match to that used
        /// by a ViewBox to size text contents.
        /// </summary>
        private const double c_heightToFontRatio = 0.47;

        /// <summary>
        /// Define a default size just in case we end up with 0, as
        /// a 0 font size will cause errors
        /// </summary>
        private const double c_defaultFontSize = 12;
    }
}
