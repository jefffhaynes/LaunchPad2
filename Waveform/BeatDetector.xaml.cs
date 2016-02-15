using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Drawing.Color;

namespace Waveform
{
    public partial class BeatDetector : UserControl
    {
        private const int SubbandCount = 32;

        public static readonly DependencyProperty EnergySubbandsProperty = DependencyProperty.Register(
            "EnergySubbands", typeof (IEnumerable<IEnumerable<double>>), typeof (BeatDetector),
            new PropertyMetadata(default(IEnumerable<IEnumerable<double>>), EnergySubbandsChangedCallback));

        public static readonly DependencyProperty TimeScaleProperty = DependencyProperty.Register(
            "TimeScale", typeof (double), typeof (BeatDetector),
            new PropertyMetadata(default(double), TimeScalePropertyChangedCallback));

        public static readonly DependencyProperty SamplePositionProperty = DependencyProperty.Register(
            "SamplePosition", typeof (uint), typeof (BeatDetector),
            new FrameworkPropertyMetadata(default(uint), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        private readonly ScaleTransform _timeScale = new ScaleTransform();

        public BeatDetector()
        {
            InitializeComponent();

            MouseLeftButtonDown += (sender, args) =>
            {
                Point position = args.GetPosition(this);
                var sample = (uint) (position.X/TimeScale);
                SamplePosition = sample;
            };
        }

        public IEnumerable<IEnumerable<double>> EnergySubbands
        {
            get { return (IEnumerable<IEnumerable<double>>) GetValue(EnergySubbandsProperty); }
            set { SetValue(EnergySubbandsProperty, value); }
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

        private static void TimeScalePropertyChangedCallback(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs e)
        {
            var control = (BeatDetector) dependencyObject;
            control._timeScale.ScaleX = (double) e.NewValue;
        }

        private static void EnergySubbandsChangedCallback(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs e)
        {
            var control = (BeatDetector) dependencyObject;
            control.EnergySubbandsChangedCallback(e);
        }

        private void EnergySubbandsChangedCallback(DependencyPropertyChangedEventArgs e)
        {
            SamplesContainer.Items.Clear();

            var samples = (IEnumerable<IEnumerable<double>>) e.NewValue;

            Task.Run(() => DrawSubbands(samples));
        }

        private void DrawSubbands(IEnumerable<IEnumerable<double>> subbands)
        {
            if (subbands == null)
                return;

            List<IEnumerable<double>> subbandList = subbands.Take(SubbandCount).ToList();

            subbandList.Reverse();

            if (subbandList.Count == 0)
                return;

            foreach (var subband in subbandList)
            {
                double[] subbandData = subband.ToArray();

                Dispatcher.Invoke(() =>
                {
                    Array.Resize(ref subbandData, subbandData.Length);

                    WriteableBitmap bitmap = BitmapFactory.New(subbandData.Length, 1);

                    IEnumerable<byte[]> subbandPixels = subbandData.Select(power =>
                    {
                        double hue = (power*256 + 240)%byte.MaxValue;
                        double value = Math.Min(Math.Max(power*32, 0.0), 1.0);
                        Color color = ColorFromHsv(hue, 1.0, value);
                        return new[] {color.B, color.G, color.R, color.A};
                    });

                    byte[] subbandValues = subbandPixels.SelectMany(pixel => pixel).ToArray();
                    int stride = bitmap.Format.BitsPerPixel/8*subbandData.Length;

                    bitmap.WritePixels(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight),
                        subbandValues, stride, 0);

                    bitmap.Freeze();

                    SamplesContainer.Items.Add(new Image
                    {
                        Stretch = Stretch.Fill,
                        Source = bitmap,
                    });
                });
            }
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