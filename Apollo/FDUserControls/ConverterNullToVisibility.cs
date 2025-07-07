//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! ConverterNullToVisibility 
//
//! Author:     Alan MacAree
//! Created:    12 Dec 2022
//
// ! if value == null, then Visibility.Collapsed is returned.
// ! if value != null, then Visibility.Visable is returned.
//
// ! Note, the above logic is reversed by the use of the IsReversed variable.
// ! The IsReversed variable can be set from within XAML, e.g.
// ! <local:ConverterNullToVisibility x:Key="ReversedNullToVisibilityConverter" IsReversed="True"></local:ConverterNullToVisibility>
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
    /// A reversible null to Visibility converter.
    /// The conversion can be reversed by setting IsReversed = True.
    /// </summary>
    public sealed class ConverterNullToVisibility : IValueConverter
    {
        /// <summary>
        /// Reverses this converter so that null results in Collapsed and
        /// !null results in Visible.
        /// </summary>
        public bool IsReversed { get; set; } = false;

        /// <summary>
        /// Converts a null to Visibility.Visible or Visibility.Collapsed.
        /// The direction of conversion is dependant on IsReversed:
        /// IsReversed == false (default)
        ///     !null == Visibility.Visible 
        ///     null == Visibility.Collapsed
        ///  IsReversed == true   
        ///     !null == Visibility.Collapsed 
        ///     null == Visibility.Visible
        /// </summary>
        /// <param name="value">The value to check if it is null</param>
        /// <param name="targetType">Not used</param>
        /// <param name="parameter">Not used</param>
        /// <param name="culture">Not used</param>
        /// <returns>Visibility.Visible or Visibility.Collapsed</returns>
        public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
        {
            Visibility visibility = Visibility.Collapsed;
            
            if ( value != null )
            {
                // Do we need to reverse the result?
                if ( !IsReversed )
                {
                    visibility = Visibility.Visible;
                }
            }
            else
            {
                // Do we need to reverse the result?
                if ( IsReversed )
                {
                    visibility = Visibility.Visible;
                }
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

