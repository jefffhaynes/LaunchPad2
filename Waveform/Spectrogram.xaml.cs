using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FMOD;
using Color = System.Drawing.Color;

namespace Waveform
{
    public partial class Spectrogram : UserControl
    {
        const int WindowSize = 256;

        public static readonly DependencyProperty SamplesProperty = DependencyProperty.Register(
            "Samples", typeof (IEnumerable<StereoSample>), typeof (Spectrogram),
            new PropertyMetadata(default(IEnumerable<StereoSample>), SamplesPropertyChangedCallback));

        public static readonly DependencyProperty TimeScaleProperty = DependencyProperty.Register(
            "TimeScale", typeof (double), typeof (Spectrogram),
            new PropertyMetadata(default(double), TimeScalePropertyChangedCallback));

        public static readonly DependencyProperty SamplePositionProperty = DependencyProperty.Register(
            "SamplePosition", typeof (uint), typeof (Spectrogram),
            new FrameworkPropertyMetadata(default(uint), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        private static readonly LomontFft Fft = new LomontFft();

        private readonly ScaleTransform _timeScale = new ScaleTransform();

        public Spectrogram()
        {
            InitializeComponent();

            MouseLeftButtonDown += (sender, args) =>
            {
                Point position = args.GetPosition(this);
                var sample = (uint) (position.X/TimeScale);
                SamplePosition = sample;
            };
        }

        public uint SamplePosition
        {
            get { return (uint) GetValue(SamplePositionProperty); }
            set { SetValue(SamplePositionProperty, value); }
        }

        public double TimeScale
        {
            get { return (double) GetValue(TimeScaleProperty); }
            set { SetValue(TimeScaleProperty, value); }
        }

        public IEnumerable<StereoSample> Samples
        {
            get { return (IEnumerable<StereoSample>) GetValue(SamplesProperty); }
            set { SetValue(SamplesProperty, value); }
        }

        private static void TimeScalePropertyChangedCallback(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs e)
        {
            var control = (Spectrogram) dependencyObject;
            control._timeScale.ScaleX = (double) e.NewValue;
        }

        private static void SamplesPropertyChangedCallback(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs e)
        {
            var control = (Spectrogram) dependencyObject;
            control.SamplesPropertyChangedCallback(e);
        }

        private void SamplesPropertyChangedCallback(DependencyPropertyChangedEventArgs e)
        {
            SamplesContainer.Items.Clear();

            var samples = (IEnumerable<StereoSample>) e.NewValue;

            Task.Run(() => DrawSamples(samples));
        }

        private static IEnumerable<double[]> GetWindows(IEnumerable<StereoSample> samples)
        {
            IEnumerable<IEnumerable<StereoSample>> windows = samples.Batch(WindowSize);

            IEnumerable<double[]> complexWindows = windows.Select(window =>
            {
                StereoSample[] data = window.ToArray();
                Array.Resize(ref data, WindowSize);

                IEnumerable<double> averageData = data.Select(sample => (double) sample.Left);
                IEnumerable<double> zeroData = Enumerable.Repeat(0.0, WindowSize);

                double[] complexData = averageData.Interleave(zeroData).ToArray();

                Fft.RealFFT(complexData, true);

                IEnumerable<double> realSpectralData = complexData.TakeEvery(2);
                IEnumerable<double> imaginarySpectralData = complexData.Skip(1).TakeEvery(2);

                IEnumerable<double> spectralMagData = realSpectralData.Zip(imaginarySpectralData,
                    (r, i) => Math.Sqrt(r*r + i*i));

                return spectralMagData.ToArray();
            });

            return complexWindows;
        }

        private void DrawSamples(IEnumerable<StereoSample> samples)
        {
            ParallelQuery<double[]> windows = GetWindows(samples).AsParallel().AsOrdered();

            IEnumerable<IEnumerable<double[]>> windowBatches = windows.Batch(16);

            foreach (var windowBatch in windowBatches)
            {
                double[][] dispatcherWindowBatch = windowBatch.ToArray();
                Dispatcher.Invoke(() => DrawWindowBatch(dispatcherWindowBatch));
            }
        }

        private void DrawWindowBatch(double[][] windowBatch)
        {
            int width = windowBatch.Sum(window => window.Length);

            WriteableBitmap bitmap = BitmapFactory.New(width, 256);

            int windowOffset = 0;
            foreach (var window in windowBatch)
            {
                double step = bitmap.Height/window.Length;
                var height = (int) Math.Ceiling(step);

                for (int i = 0; i < window.Length; i++)
                {
                    double power = window[i];
                    power *= 256;
                    double hue = (power + 240)%byte.MaxValue;

                    if (power > 10)
                    {
                        Color color = ColorFromHsv(hue, 1.0, 1.0);

                        const int border = 6;

                        var y = (int) (i*step);
                        bitmap.DrawRectangle(windowOffset, y, windowOffset + window.Length - border, y + height - border,
                            System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
                    }
                }

                windowOffset += window.Length;
            }

            bitmap.Freeze();

            var viewBox = new NonuniformViewbox
            {
                StretchOrientation = StretchOrientation.Vertical,
                Child = new Image
                {
                    Stretch = Stretch.None,
                    Source = bitmap,
                    LayoutTransform = _timeScale
                }
            };

            SamplesContainer.Items.Add(viewBox);
        }

        private static Color ColorFromHsv(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue/60))%6;
            double f = hue/60 - Math.Floor(hue/60);

            value = value*255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value*(1 - saturation));
            int q = Convert.ToInt32(value*(1 - f*saturation));
            int t = Convert.ToInt32(value*(1 - (1 - f)*saturation));

            if (hi == 0)
                return Color.FromArgb(255, v, t, p);
            if (hi == 1)
                return Color.FromArgb(255, q, v, p);
            if (hi == 2)
                return Color.FromArgb(255, p, v, t);
            if (hi == 3)
                return Color.FromArgb(255, p, q, v);
            if (hi == 4)
                return Color.FromArgb(255, t, p, v);
            return Color.FromArgb(255, v, p, q);
        }
    }
}