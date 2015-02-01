using System;
using System.Globalization;
using System.Windows.Data;
using LaunchPad2.Annotations;

namespace LaunchPad2.Converters
{
    public class SampleDecimationConverter : IValueConverter
    {
        public SampleDecimationConverter()
        {
            _decimation = Settings.Default.Decimation;
        }

        private readonly double _decimation;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var sample = (uint) value;
            return (uint)(sample*_decimation);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var decimatedSample = (uint) value;
            return (uint)(decimatedSample/_decimation);
        }
    }
}
