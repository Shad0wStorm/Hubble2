//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! ConverterBoolToVisibility 
//
//! Used to convert a bool value (True or False) into a 
//! visibility (Visibility.Visible or Visibility.Collapsed)
//
//! Author:     Alan MacAree
//! Created:    16 Sep 2022
//
// ! if value == true, then Visibility.Visible is returned.
// ! if value == false, then Visibility.Collapsed is returned.
//
// ! Note, the above logic is reversed by the use of the IsReversed variable.
// ! The IsReversed variable can be set from within XAML, e.g.
// ! <local:ConverterBoolToVisibility x:Key="ReversedBoolToVisibilityConverter" IsReversed="True"></local:ConverterBoolToVisibility>
//
//----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FDUserControls
{
    /// <summary>
    /// A reversible bool to Visibility converter.
    /// The conversion can be reversed by setting IsReversed = True.
    /// </summary>
    public sealed class ConverterBoolToVisibility : IValueConverter
    {
        /// <summary>
        /// Reverses this converter so that True results in Collapsed and
        /// False results in Visible.
        /// </summary>
        public bool IsReversed { get; set; } = false;

        /// <summary>
        /// Converts a bool to Visibility.Visible or Visibility.Collapsed.
        /// The direction of conversion is dependant on IsReversed:
        /// IsReversed == false (default)
        ///     true == Visibility.Visible 
        ///     false == Visibility.Collapsed
        ///  IsReversed == true   
        ///     true == Visibility.Collapsed 
        ///     false == Visibility.Visible
        /// </summary>
        /// <param name="value">True or False, this is the bool that is converted</param>
        /// <param name="targetType">Not used</param>
        /// <param name="parameter">Not used</param>
        /// <param name="culture">Not used</param>
        /// <returns>Visibility.Visible or Visibility.Collapsed</returns>
        public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
        {
            Visibility visibility = Visibility.Collapsed;
            bool valueAsBool = false;

            // If we can convert the value to a bool, then convert it.
            if ( bool.TryParse( value.ToString(), out valueAsBool ) )
            {
                // Do we ned to reverse the result?
                if ( IsReversed )
                {
                    valueAsBool = !valueAsBool;
                }
            }
            else
            {
                // Cause an assert to fail if we get here in debug, 
                // otherwise just ignore it.
                Debug.Assert( false );
            }

            // Set the result based on the value of valueAsBool
            if ( valueAsBool )
            {
                visibility = Visibility.Visible;
            }

            return visibility;
        }

        /// <summary>
        /// Not implemented
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
