using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Color = System.Drawing.Color;

namespace LaunchPad2.Converters
{
    public class LighteningConverter : BrushConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var brush = value as SolidColorBrush;
            if (brush == null)
                return value;

            var color = brush.GetColor();

            var factor = System.Convert.ToDouble(parameter ?? 0.25);
            var lightColor = color.Lighten(factor);

            return lightColor.GetSolidColorBrush();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}