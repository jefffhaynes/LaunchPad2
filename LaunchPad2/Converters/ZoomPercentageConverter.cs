using System;
using System.Globalization;
using System.Windows.Data;
using LaunchPad2.Annotations;

namespace LaunchPad2.Converters
{
    public class ZoomPercentageConverter : IValueConverter
    {
        private readonly double _decimation;

        public ZoomPercentageConverter()
        {
            _decimation = Settings.Default.Decimation;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var percent = (double)value / _decimation / 10;
            return string.Format("{0:P}", percent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
