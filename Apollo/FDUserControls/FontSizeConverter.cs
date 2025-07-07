//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! FontSizeConverter 
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
    /// Converts a height into an appropriate fontsize
    /// </summary>
    class FontSizeConverter : IValueConverter
    {
        private const double c_HeightToFontRatio = 0.715;
        private const double c_DefaultFontSize = 12;

        /// <summary>
        /// Convert a control height to a font size that will
        /// fit the available height. Used in place of Viewbox
        /// when a Viewbox is not appropriate. 
        /// </summary>
        /// <param name="value">ActualHeight from the XAML</param>
        /// <param name="targetType"></param>
        /// <param name="parameter">ConverterParameter from the XAML</param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
        {
            double fontSize = 0d;

            double height = 0d;
            if ( double.TryParse( value.ToString(), out height ) )
            {
                fontSize = height * c_HeightToFontRatio;
            }
            else
            {
                // Cause an assert to fail is we get here, otherwises just ignore it.
                Debug.Assert( false );
            }

            // When a TextBox is created, it can have a zero height, causing a zero FontSize,
            // from the calulcation above. this in turn will cause a debug message:
            //  System.Windows.Data Error: 5 : Value produced by BindingExpression ...
            //
            // Using a default font will remove this message, and the FontSize is not used.
            if ( fontSize == 0 )
            {
                fontSize = c_DefaultFontSize;
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
    }
}
