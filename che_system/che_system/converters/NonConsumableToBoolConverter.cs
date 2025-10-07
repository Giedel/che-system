//-- NonConsumableToBoolConverter.cs --

using System;
using System.Globalization;
using System.Windows.Data;

namespace che_system.converters
{
    public class NonConsumableToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is string type && type == "non-consumable";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}