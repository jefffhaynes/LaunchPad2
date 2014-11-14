using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace LaunchPad2.Converters
{
    public class FileTitleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var filename = (string) value;
            if (filename == null)
                return "Untitled";
            return Path.GetFileNameWithoutExtension(filename);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
