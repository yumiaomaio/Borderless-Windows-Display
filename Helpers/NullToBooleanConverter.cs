using System;
using System.Globalization;
using System.Windows; 
using System.Windows.Data; 

namespace BorderlessWindowApp.Helpers
{
    /// <summary>
    /// Converts null to false and non-null to true.
    /// Can be inverted by passing "Invert" as the ConverterParameter.
    /// Useful for enabling/disabling controls based on whether an object is selected.
    /// </summary>
    public class NullToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isNull = value == null;
            bool invert = parameter as string == "Invert"; // Check if "Invert" parameter is passed

            // If invert is true, return true when value is null, false otherwise
            // If invert is false (or parameter is not "Invert"), return false when value is null, true otherwise
            return invert ? isNull : !isNull;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // ConvertBack is typically not needed for IsEnabled bindings
            throw new NotImplementedException();
        }
    }
}