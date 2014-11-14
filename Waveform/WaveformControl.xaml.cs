using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FMOD;

namespace Waveform
{
    public partial class WaveformControl : UserControl
    {
        private const int BlockSize = 10000;
        private const int WaveformHeight = 256;
        private const double YScale = 256;

        private static readonly Color DefaultLeftColor = Color.FromArgb(205, 0, 121, 203);
        private static readonly Color DefaultRightColor = Color.FromArgb(205, 228, 20, 0);

        public static readonly DependencyProperty SamplesProperty = DependencyProperty.Register(
            "Samples", typeof (IEnumerable<StereoSample>), typeof (WaveformControl),
            new PropertyMetadata(default(IEnumerable<StereoSample>), SamplesPropertyChangedCallback));

        public static readonly DependencyProperty LeftColorProperty = DependencyProperty.Register(
            "LeftColor", typeof (Color), typeof (WaveformControl),
            new PropertyMetadata(DefaultLeftColor));

        public static readonly DependencyProperty RightColorProperty = DependencyProperty.Register(
            "RightColor", typeof (Color), typeof (WaveformControl),
            new PropertyMetadata(DefaultRightColor));

        public static readonly DependencyProperty TimeScaleProperty = DependencyProperty.Register(
            "TimeScale", typeof (double), typeof (WaveformControl),
            new PropertyMetadata(default(double), TimeScalePropertyChangedCallback));

        public static readonly DependencyProperty SamplePositionProperty = DependencyProperty.Register(
            "SamplePosition", typeof (uint), typeof (WaveformControl), 
            new FrameworkPropertyMetadata(default(uint), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public uint SamplePosition
        {
            get { return (uint) GetValue(SamplePositionProperty); }
            set { SetValue(SamplePositionProperty, value); }
        }

        private readonly ScaleTransform _timeScale = new ScaleTransform();

        public WaveformControl()
        {
            InitializeComponent();

            MouseLeftButtonDown += (sender, args) =>
            {
                Point position = args.GetPosition(this);
                var sample = (uint) (position.X/TimeScale);
                SamplePosition = sample;
            };
        }

        public double TimeScale
        {
            get { return (double) GetValue(TimeScaleProperty); }
            set { SetValue(TimeScaleProperty, value); }
        }

        public Color LeftColor
        {
            get { return (Color) GetValue(LeftColorProperty); }
            set { SetValue(LeftColorProperty, value); }
        }

        public Color RightColor
        {
            get { return (Color) GetValue(RightColorProperty); }
            set { SetValue(RightColorProperty, value); }
        }

        public IEnumerable<StereoSample> Samples
        {
            get { return (IEnumerable<StereoSample>) GetValue(SamplesProperty); }
            set { SetValue(SamplesProperty, value); }
        }

        private static void TimeScalePropertyChangedCallback(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs e)
        {
            var control = (WaveformControl) dependencyObject;
            control._timeScale.ScaleX = (double) e.NewValue;
        }

        private static void SamplesPropertyChangedCallback(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs e)
        {
            var control = (WaveformControl) dependencyObject;
            control.SamplesPropertyChangedCallback(e);
        }

        private void SamplesPropertyChangedCallback(DependencyPropertyChangedEventArgs e)
        {
            SamplesContainer.Items.Clear();

            var samples = (IEnumerable<StereoSample>) e.NewValue;

            if(samples != null)
                Task.Run(() => DrawSamples(samples));
        }

        private void DrawSamples(IEnumerable<StereoSample> samples)
        {
            IEnumerable<IEnumerable<StereoSample>> drawingBlocks = samples.Batch(BlockSize);

            foreach (var drawingBlock in drawingBlocks)
            {
                StereoSample[] drawingBlockData = drawingBlock.ToArray();

                List<int> timeSequence = Enumerable.Range(0, drawingBlockData.Length).ToList();
                List<float> leftValues = drawingBlockData.Select(sample => sample.Left).ToList();
                List<float> rightValues = drawingBlockData.Select(sample => sample.Right).ToList();

                const int yOffset = WaveformHeight/2;

                IEnumerable<int> leftValuesAdjusted = leftValues.Select(sample => (int)(sample * YScale + yOffset));
                IEnumerable<int> rightValuesAdjusted = rightValues.Select(sample => (int) (sample*YScale + yOffset));

                IEnumerable<int> leftPoints = timeSequence.Interleave(leftValuesAdjusted);
                IEnumerable<int> rightPoints = timeSequence.Interleave(rightValuesAdjusted);

                Dispatcher.Invoke(() =>
                {
                    WriteableBitmap bitmap = BitmapFactory.New(drawingBlockData.Length, WaveformHeight);

                    bitmap.DrawPolyline(leftPoints.ToArray(), LeftColor);
                    bitmap.DrawPolyline(rightPoints.ToArray(), RightColor);
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
                });
            }
        }
    }
}