using System;
using System.Globalization;
using System.Windows.Data;

namespace che_system.converters
{
    public class ConsumableCompletedToEnabledConverter : IMultiValueConverter
    {
        // Returns true to keep row enabled; false to disable it when consumable fully released.
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var typeStr = values.Length > 0 ? values[0]?.ToString() : null;
                bool isConsumable = string.Equals(typeStr, "consumable", StringComparison.OrdinalIgnoreCase);

                int borrowed = 0;
                if (values.Length > 1 && values[1] != null)
                    borrowed = values[1] is int ib ? ib : System.Convert.ToInt32(values[1], CultureInfo.InvariantCulture);

                int released = 0;
                if (values.Length > 2 && values[2] != null && values[2] != System.Windows.DependencyProperty.UnsetValue)
                    released = values[2] is int ir ? ir : System.Convert.ToInt32(values[2], CultureInfo.InvariantCulture);

                // Disable only if consumable and fully released (released >= borrowed) and borrowed > 0
                if (isConsumable && borrowed > 0 && released >= borrowed)
                    return false;

                return true;
            }
            catch
            {
                // On any conversion issue, keep enabled to avoid blocking the UI
                return true;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}