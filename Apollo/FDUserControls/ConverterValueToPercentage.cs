//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! ConverterValueToPercentage
//
//! Author:     Alan MacAree
//! Created:    08 Nov 2022
//
// Converts a Value to a Percentage, given a total.
//----------------------------------------------------------------------

using System;
using System.Globalization;
using System.Windows.Data;

namespace FDUserControls
{
    /// <summary>
    /// Converts a Value to a Percentage, given a total.
    /// </summary>
    public class ConverterValueToPercentage : IMultiValueConverter
    {
        /// <summary>
        /// Converts a value and a total into a percentage
        /// </summary>
        /// <param name="values">Array, must contain 2 values and only 2 values</param>
        /// <param name="targetType">Not used</param>
        /// <param name="parameter">Extra optional string that is appended to the end of the value returned</param>
        /// <param name="culture">Not used</param>
        /// <returns>The percentage that values[0] is against the total values[1] + the optional parameter</returns>
        public object Convert( object[] values, Type targetType, object parameter, CultureInfo culture )
        {
            string stringResult = "";

            // We must have 2 values, the value and the total, this never changes (no point making it a const)
            if ( values.Length == 2 )
            {
                long valueAsLong = 0;
                if ( values[0]  != null && values[1] != null )
                { 
                    if ( long.TryParse( values[0].ToString(), out valueAsLong ) )
                    {
                        long totalAsLong = 0;
                        if ( long.TryParse( values[1].ToString(), out totalAsLong ) )
                        {
                            if ( valueAsLong >= 0 && totalAsLong > 0 )
                            {
                                double value2 = (double)valueAsLong / (double)totalAsLong;
                                double value3 = value2 * 100;
                                stringResult = ((int)value3).ToString();
                                stringResult += "%";
                                if ( parameter != null )
                                {
                                    stringResult += parameter.ToString();
                                }
                            }
                        }
                    }
                }
            }

            return stringResult;
        }

        /// <summary>
        /// Not impelmented
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object[] ConvertBack( object value, Type[] targetTypes, object parameter, CultureInfo culture )
        {
            throw new NotImplementedException();
        }
    }
}

