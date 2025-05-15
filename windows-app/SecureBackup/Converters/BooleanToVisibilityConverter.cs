using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SecureBackup.Converters
{
    /// <summary>
    /// Converts a boolean value to a Visibility value
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BooleanToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Convert a boolean value to a Visibility value
        /// </summary>
        /// <param name="value">The boolean value to convert</param>
        /// <param name="targetType">The type of the binding target property</param>
        /// <param name="parameter">Optional parameter to invert the conversion</param>
        /// <param name="culture">The culture to use in the converter</param>
        /// <returns>Visibility.Visible if true, Visibility.Collapsed if false</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // Check if we should invert the conversion
                bool invert = parameter != null && bool.TryParse(parameter.ToString(), out bool invertValue) && invertValue;
                
                if (invert)
                {
                    return boolValue ? Visibility.Collapsed : Visibility.Visible;
                }
                
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            
            return Visibility.Collapsed;
        }

        /// <summary>
        /// Convert a Visibility value back to a boolean value
        /// </summary>
        /// <param name="value">The Visibility value to convert</param>
        /// <param name="targetType">The type of the binding target property</param>
        /// <param name="parameter">Optional parameter to invert the conversion</param>
        /// <param name="culture">The culture to use in the converter</param>
        /// <returns>True if Visible, False if Collapsed or Hidden</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                // Check if we should invert the conversion
                bool invert = parameter != null && bool.TryParse(parameter.ToString(), out bool invertValue) && invertValue;
                
                bool result = visibility == Visibility.Visible;
                
                if (invert)
                {
                    return !result;
                }
                
                return result;
            }
            
            return false;
        }
    }
}
