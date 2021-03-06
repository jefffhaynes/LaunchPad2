﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using FMOD;
using LaunchPad2.Annotations;
using LaunchPad2.Properties;
using Waveform;

namespace LaunchPad2.Converters
{
    public class SamplesDecimationConverter : IValueConverter
    {
        private readonly double _decimation;

        public SamplesDecimationConverter()
        {
            _decimation = Settings.Default.Decimation;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var decimationInterval = (int) (1/_decimation);

            var samples = value as IEnumerable<StereoSample>;

            if (samples != null)
                return samples.TakeEvery(decimationInterval);

            var simpleSamples = value as IEnumerable<double>;

            return simpleSamples?.TakeEvery(decimationInterval);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}