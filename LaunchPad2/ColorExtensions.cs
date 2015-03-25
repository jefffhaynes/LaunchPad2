using System.Windows.Media;
using Color = System.Drawing.Color;

namespace LaunchPad2
{
    public static class ColorExtensions
    {
        public static Color GetColor(this SolidColorBrush solidColorBrush)
        {
            var c = solidColorBrush.Color;
            return Color.FromArgb(c.A, c.R, c.G, c.B);
        }

        public static SolidColorBrush GetSolidColorBrush(this Color color)
        {
            return new SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
        }

        public static Color Lighten(this Color color, double factor)
        {
            double red = (255 - color.R) * factor + color.R;
            double green = (255 - color.G) * factor + color.G;
            double blue = (255 - color.B) * factor + color.B;
            return Color.FromArgb(color.A, (int)red, (int)green, (int)blue);
        }
    }
}
