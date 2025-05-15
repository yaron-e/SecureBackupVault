using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SecureBackup.Converters
{
    /// <summary>
    /// Converts a numeric value to a Visibility value based on whether it equals zero
    /// </summary>
    [ValueConversion(typeof(int), typeof(Visibility))]
    public class ZeroToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Convert a numeric value to a Visibility value
        /// </summary>
        /// <param name="value">The numeric value to convert</param>
        /// <param name="targetType">The type of the binding target property</param>
        /// <param name="parameter">Optional parameter to invert the conversion</param>
        /// <param name="culture">The culture to use in the converter</param>
        /// <returns>Visibility.Visible if zero, Visibility.Collapsed if non-zero</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return Visibility.Visible; // Consider null as zero
            }
            
            // Try to convert to numeric value
            bool isZero = false;
            
            if (value is int intValue)
            {
                isZero = intValue == 0;
            }
            else if (value is long longValue)
            {
                isZero = longValue == 0;
            }
            else if (value is double doubleValue)
            {
                isZero = doubleValue == 0;
            }
            else if (value is decimal decimalValue)
            {
                isZero = decimalValue == 0;
            }
            else if (int.TryParse(value.ToString(), out int parsedValue))
            {
                isZero = parsedValue == 0;
            }
            
            // Check if we should invert the conversion
            bool invert = parameter != null && bool.TryParse(parameter.ToString(), out bool invertValue) && invertValue;
            
            if (invert)
            {
                return isZero ? Visibility.Collapsed : Visibility.Visible;
            }
            
            return isZero ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Convert back is not implemented
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ConvertBack not implemented for ZeroToVisibilityConverter");
        }
    }
}
