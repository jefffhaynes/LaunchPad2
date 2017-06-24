using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LaunchPad2.Annotations;
using LaunchPad2.Properties;

namespace LaunchPad2.Controls
{
    public abstract class CueControlBase : Control
    {
        private const double AutoScrollOuterLimit = 64;
        private static readonly TimeSpan MinCueLength = TimeSpan.FromSeconds(0.5);

        public static readonly DependencyProperty SampleProperty = DependencyProperty.Register(
            "Sample", typeof(uint), typeof(CueControlBase), new FrameworkPropertyMetadata(default(uint),
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SampleChangedCallback));

        public static readonly DependencyProperty SampleLengthProperty = DependencyProperty.Register(
            "SampleLength", typeof(int), typeof(CueControlBase),
            new FrameworkPropertyMetadata(default(int),
                FrameworkPropertyMetadataOptions.AffectsParentMeasure |
                FrameworkPropertyMetadataOptions.AffectsArrange |
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                SampleLengthChangedCallback, CoerceSampleLengthCallback));

        public static readonly DependencyProperty SampleRateProperty = DependencyProperty.Register(
            "SampleRate", typeof(float), typeof(CueControlBase), new PropertyMetadata(default(float)));

        public static readonly DependencyProperty TimeScaleProperty = DependencyProperty.Register(
            "TimeScale", typeof(double), typeof(CueControlBase),
            new PropertyMetadata(1.0, TimeScaleChangedCallback));

        public static readonly DependencyProperty AutoscrollDistanceProperty = DependencyProperty.Register(
            "AutoscrollDistance", typeof(double), typeof(CueControlBase), new PropertyMetadata(60.0));

        public static readonly DependencyProperty SnapToIntervalProperty = DependencyProperty.Register(
            "SnapToInterval", typeof(TimeSpan), typeof(CueControlBase),
            new PropertyMetadata(TimeSpan.FromMilliseconds(100)));

        public static readonly DependencyProperty OffsetProperty = DependencyProperty.Register(
            "Offset", typeof(double), typeof(CueControlBase), new PropertyMetadata(default(double)));

        public static readonly DependencyProperty CanResizeProperty = DependencyProperty.Register(
            "CanResize", typeof(bool), typeof(CueControlBase), new PropertyMetadata(true));

        private Canvas _canvas;
        private ScrollViewer _scrollViewer;


        public float SampleRate
        {
            get { return (float) GetValue(SampleRateProperty); }
            set { SetValue(SampleRateProperty, value); }
        }

        public bool CanResize
        {
            get { return (bool) GetValue(CanResizeProperty); }
            set { SetValue(CanResizeProperty, value); }
        }

        public double Offset
        {
            get { return (double) GetValue(OffsetProperty); }
            set { SetValue(OffsetProperty, value); }
        }

        public TimeSpan SnapToInterval
        {
            get { return (TimeSpan) GetValue(SnapToIntervalProperty); }
            set { SetValue(SnapToIntervalProperty, value); }
        }

        public double AutoscrollDistance
        {
            get { return (double) GetValue(AutoscrollDistanceProperty); }
            set { SetValue(AutoscrollDistanceProperty, value); }
        }

        public uint Sample
        {
            get { return (uint) GetValue(SampleProperty); }
            set { SetValue(SampleProperty, value); }
        }

        public int SampleLength
        {
            get { return (int) GetValue(SampleLengthProperty); }
            set { SetValue(SampleLengthProperty, value); }
        }

        public double TimeScale
        {
            get { return (double) GetValue(TimeScaleProperty); }
            set { SetValue(TimeScaleProperty, value); }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            double width = constraint.Width;
            double height = constraint.Height;

            Size naturalSize = GetNaturalSize();

            if (double.IsInfinity(width) && double.IsInfinity(height))
                return GetNaturalSize();

            if (double.IsInfinity(width))
                return new Size(naturalSize.Width, height);

            if (double.IsInfinity(height))
                return new Size(width, naturalSize.Height);

            width = Math.Max(width, naturalSize.Width);
            height = Math.Max(height, naturalSize.Height);

            return new Size(width, height);
        }

        private static void TimeScaleChangedCallback(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs e)
        {
            var control = (CueControlBase)dependencyObject;
            control.TimeScaleChangedCallback(e);
        }

        protected virtual void TimeScaleChangedCallback(DependencyPropertyChangedEventArgs e)
        {
            double offset = Sample*(double) e.NewValue;
            SetLeft(offset);
        }

        private static void SampleChangedCallback(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs e)
        {
            var control = (CueControlBase)dependencyObject;
            control.SampleChangedCallback(e);
        }

        private void SampleChangedCallback(DependencyPropertyChangedEventArgs e)
        {
            double left = (uint) e.NewValue*TimeScale;
            SetLeft(left);

            AutoScrollStep();
        }

        private static void SampleLengthChangedCallback(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs e)
        {
            var control = (CueControlBase)dependencyObject;
            control.SampleLengthChangedCallback(e);
        }

        private void SampleLengthChangedCallback(DependencyPropertyChangedEventArgs e)
        {
            double length = (int) e.NewValue*TimeScale;
            SetWidth(length);
        }

        protected void AutoScrollStep()
        {
            if (_scrollViewer == null)
                _scrollViewer = FindScrollViewer();

            if (_scrollViewer == null)
                return;

            if (_canvas == null)
                _canvas = FindAncestor<Canvas>();

            const double autoscrollEase = 0.1;

            double referenceOffset = GetAutoscrollReferenceOffset();

            double canvasRight = _scrollViewer.HorizontalOffset + _scrollViewer.ViewportWidth;
            double mouseRight = canvasRight - referenceOffset;

            if (mouseRight < -AutoScrollOuterLimit)
                return;

            if (mouseRight < AutoscrollDistance)
            {
                double delta = AutoscrollDistance - mouseRight;
                _scrollViewer.ScrollToHorizontalOffset(_scrollViewer.HorizontalOffset + delta*autoscrollEase);
            }
            else
            {
                double mouseLeft = referenceOffset - _scrollViewer.HorizontalOffset;

                if (mouseLeft < AutoscrollDistance && mouseLeft > -AutoScrollOuterLimit)
                {
                    double delta = mouseLeft - AutoscrollDistance;
                    _scrollViewer.ScrollToHorizontalOffset(_scrollViewer.HorizontalOffset + delta*autoscrollEase);
                }
            }
        }

        protected virtual double GetAutoscrollReferenceOffset()
        {
            return Mouse.GetPosition(_canvas).X;
        }

        protected virtual double Snap(double value)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                return value;

            int samples = ToSample(SnapToInterval);
            return value - value%samples;
        }

        private static object CoerceSampleLengthCallback(DependencyObject dependencyObject, object baseValue)
        {
            var control = (CueControlBase)dependencyObject;
            var sampleLength = (int) baseValue;
            return control.CoerceSampleLengthCallback(sampleLength);
        }

        protected virtual object CoerceSampleLengthCallback(int sampleLength)
        {
            int minSampleLength = ToSample(MinCueLength);
            if (sampleLength < minSampleLength)
                return minSampleLength;

            return sampleLength;
        }

        protected int ToSample(TimeSpan timeSpan)
        {
            double totalSeconds = timeSpan.TotalMilliseconds/1000;
            var samples = (int) (totalSeconds*SampleRate);
            return (int)(samples*Settings.Default.Decimation);
        }

        protected virtual void SetLeft(double left)
        {
            Offset = left;
            SetValue(Canvas.LeftProperty, left);
        }

        protected virtual double GetLeft()
        {
            return (double) GetValue(Canvas.LeftProperty);
        }

        protected virtual void SetWidth(double width)
        {
            throw new NotImplementedException();
        }

        protected virtual double GetWidth()
        {
            return GetNaturalSize().Width;
        }

        protected abstract Size GetNaturalSize();

        internal Size GetNaturalScaledSize()
        {
            Size naturalSize = GetNaturalSize();
            return new Size(naturalSize.Width/TimeScale, naturalSize.Height);
        }

        internal ScrollViewer FindScrollViewer()
        {
            return FindAncestor<ScrollViewer>();
        }

        protected T FindAncestor<T>() where T : DependencyObject
        {
            return UiHelper.FindAncestor<T>(this);
        }
    }
}