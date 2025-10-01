using System.Globalization;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace che_system.converters
{
    public class EqualWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var panel = value as TabPanel;
            if (panel == null || panel.Children.Count == 0)
                return double.NaN; // fallback to auto

            double totalWidth = panel.ActualWidth;
            int count = panel.Children.Count;

            if (count == 0 || totalWidth == 0)
                return double.NaN;

            return totalWidth / count;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
