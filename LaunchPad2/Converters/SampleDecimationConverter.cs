using System;
using System.Globalization;
using System.Windows.Data;
using LaunchPad2.Annotations;
using LaunchPad2.Properties;

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
            var sample = System.Convert.ToDouble(value);
            return sample * _decimation;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var decimatedSample = System.Convert.ToDouble(value);
            return decimatedSample/_decimation;
        }
    }
}
