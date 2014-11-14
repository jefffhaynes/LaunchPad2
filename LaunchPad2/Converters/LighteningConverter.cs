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

            var c = brush.Color;
            Color color = Color.FromArgb(c.A, c.R, c.G, c.B);

            var factor = System.Convert.ToDouble(parameter ?? 0.25);
            var lightColor = Lighten(color, factor);

            return new SolidColorBrush(System.Windows.Media.Color.FromArgb(lightColor.A, lightColor.R, lightColor.G, lightColor.B));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private static Color Lighten(Color color, double factor)
        {
            double red = (255 - color.R) * factor + color.R;
            double green = (255 - color.G) * factor + color.G;
            double blue = (255 - color.B) * factor + color.B;
            return Color.FromArgb(color.A, (int)red, (int)green, (int)blue);
        }
    }
}