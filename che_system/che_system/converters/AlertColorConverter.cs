//-- AlertColorConverter.cs --

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace che_system.converters
{
    public class AlertColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string type = value?.ToString()?.ToLower() ?? "";

            return type switch
            {
                "expiring" => new SolidColorBrush(Colors.Goldenrod),  // 🟡 Yellow
                "low stock" => new SolidColorBrush(Colors.IndianRed), // 🔴 Red
                _ => new SolidColorBrush(Colors.Gray)
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
