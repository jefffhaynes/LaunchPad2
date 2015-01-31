using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LaunchPad2.Converters;
using LaunchPad2.ViewModels;

namespace LaunchPad2.Controls
{
    public abstract class CueControl : Control
    {
        private const double AutoScrollOuterLimit = 64;
        private static readonly TimeSpan MinCueLength = TimeSpan.FromSeconds(0.5);

        public static readonly DependencyProperty SampleProperty = DependencyProperty.Register(
            "Sample", typeof (uint), typeof (CueControl), new FrameworkPropertyMetadata(default(uint),
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SampleChangedCallback));

        public static readonly DependencyProperty SampleLengthProperty = DependencyProperty.Register(
            "SampleLength", typeof (uint), typeof (CueControl),
            new FrameworkPropertyMetadata(default(uint),
                FrameworkPropertyMetadataOptions.AffectsParentMeasure |
                FrameworkPropertyMetadataOptions.AffectsArrange |
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                SampleLengthChangedCallback, CoerceSampleLengthCallback));

        public static readonly DependencyProperty TimeScaleProperty = DependencyProperty.Register(
            "TimeScale", typeof (double), typeof (CueControl),
            new PropertyMetadata(1.0, TimeScaleChangedCallback));

        public static readonly DependencyProperty AutoscrollDistanceProperty = DependencyProperty.Register(
            "AutoscrollDistance", typeof (double), typeof (CueControl), new PropertyMetadata(60.0));

        public static readonly DependencyProperty GripWidthProperty = DependencyProperty.Register(
            "GripWidth", typeof (double), typeof (CueControl), new PropertyMetadata(0.0));

        public static readonly DependencyProperty SnapToIntervalProperty = DependencyProperty.Register(
            "SnapToInterval", typeof (TimeSpan), typeof (CueControl),
            new PropertyMetadata(TimeSpan.FromMilliseconds(100)));

        public static readonly DependencyProperty OffsetProperty = DependencyProperty.Register(
            "Offset", typeof (double), typeof (CueControl), new PropertyMetadata(default(double)));

        public static readonly DependencyProperty CanResizeProperty = DependencyProperty.Register(
            "CanResize", typeof (bool), typeof (CueControl), new PropertyMetadata(true));

        private static readonly SampleDecimationConverter SampleDecimationConverter = new SampleDecimationConverter();

        private Canvas _canvas;
        private Point _canvasClickPosition;

        private double _clickLeft;
        private double _clickWidth;
        private CueMoveMode _cueMoveMode;

        private ScrollViewer _scrollViewer;

        protected CueControl()
        {
            MouseLeftButtonDown += OnLeftButtonDown;
            MouseMove += OnMouseMove;
            MouseLeftButtonUp += OnLeftButtonUp;
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

        public double GripWidth
        {
            get { return (double) GetValue(GripWidthProperty); }
            set { SetValue(GripWidthProperty, value); }
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

        public uint SampleLength
        {
            get { return (uint) GetValue(SampleLengthProperty); }
            set { SetValue(SampleLengthProperty, value); }
        }

        public double TimeScale
        {
            get { return (double) GetValue(TimeScaleProperty); }
            set { SetValue(TimeScaleProperty, value); }
        }

        protected abstract bool HasGrips { get; }

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
            var control = (CueControl) dependencyObject;
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
            var control = (CueControl) dependencyObject;
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
            var control = (EventCueControl) dependencyObject;
            control.SampleLengthChangedCallback(e);
        }

        private void SampleLengthChangedCallback(DependencyPropertyChangedEventArgs e)
        {
            double length = (uint) e.NewValue*TimeScale;
            SetWidth(length);
        }

        private async void AutoScroll()
        {
            while (IsMouseCaptured)
            {
                AutoScrollStep();
                await Task.Delay(10);
            }
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

        private void OnLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            OnLeftButtonDownProtected(sender, e);
            e.Handled = true;
        }

        protected virtual void OnLeftButtonDownProtected(object sender, MouseButtonEventArgs e)
        {
            Keyboard.Focus(this);

            CaptureMouse();
            Point clickPosition = e.GetPosition(this);

            _clickWidth = GetWidth();
            _clickLeft = GetLeft();

            /* If we're cloning, force into move mode */
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                _cueMoveMode = CueMoveMode.Normal;
            else if (HasGrips)
            {
                if (clickPosition.X < 0)
                    _cueMoveMode = CueMoveMode.LeadIn;
                else if (clickPosition.X > _clickWidth - GripWidth)
                    _cueMoveMode = CueMoveMode.RightGrip;
                else if (clickPosition.X < GripWidth)
                    _cueMoveMode = CueMoveMode.LeftGrip;
                else _cueMoveMode = CueMoveMode.Normal;
            }
            else _cueMoveMode = CueMoveMode.Normal;

            if (_canvas == null)
                _canvas = FindAncestor<Canvas>();

            _canvasClickPosition = e.GetPosition(_canvas);

            var cueMovingEventArgs = new CueMovingEventArgs(SelectionCanvas.CueMovingEvent, _cueMoveMode);
            RaiseEvent(cueMovingEventArgs);

            AutoScroll();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (IsMouseCaptured)
            {
                Point canvasPosition = e.GetPosition(_canvas);
                double dx = canvasPosition.X - _canvasClickPosition.X;
                double naturalWidth = GetNaturalSize().Width;

                if (_cueMoveMode == CueMoveMode.RightGrip && CanResize)
                {
                    double width = _clickWidth + dx;
                    double boundedWidth = Math.Max(width, naturalWidth);
                    double sampleLength = boundedWidth/TimeScale;

                    uint minSampleLength = ToSample(MinCueLength);
                    if (sampleLength < minSampleLength)
                        sampleLength = minSampleLength;

                    double snappedSampleLength = Snap(sampleLength);

                    SetValue(SampleLengthProperty, (uint) snappedSampleLength);

                    /* Work backwards to get snapped delta for routed event */
                    double snappedWidth = snappedSampleLength*TimeScale;
                    double snappedDx = snappedWidth - _clickWidth;

                    double scaledSnappedDx = snappedDx/TimeScale;
                    var cueMoveEventArgs = new CueMoveEventArgs(SelectionCanvas.CueMoveEvent, scaledSnappedDx);
                    RaiseEvent(cueMoveEventArgs);
                }
                else
                {
                    /* Move */
                    double left = _clickLeft + dx;
                    double boundedLeft = Math.Max(left, 0);
                    double sample = boundedLeft/TimeScale;

                    double snappedSample = Snap(sample);

                    /* For the audio cue */
                    if (_cueMoveMode == CueMoveMode.Normal)
                        SetValue(SampleProperty, (uint) snappedSample);

                    /* Work backwards to compensate for snapped movement */
                    double snappedLeft = snappedSample*TimeScale;
                    double snappedDx = snappedLeft - _clickLeft;

                    double scaledSnappedDx = snappedDx/TimeScale;

                    if (_cueMoveMode == CueMoveMode.Normal || CanResize)
                    {
                        var cueMoveEventArgs = new CueMoveEventArgs(SelectionCanvas.CueMoveEvent, scaledSnappedDx);
                        RaiseEvent(cueMoveEventArgs);
                    }
                }

                e.Handled = true;
            }
        }

        private void OnLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var cueMovedEventArgs = new RoutedEventArgs(SelectionCanvas.CueMovedEvent);
            RaiseEvent(cueMovedEventArgs);

            ReleaseMouseCapture();
            e.Handled = true;
        }

        protected virtual double Snap(double value)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                return value;

            uint samples = ToSample(SnapToInterval);
            return value - value%samples;
        }

        private static object CoerceSampleLengthCallback(DependencyObject dependencyObject, object baseValue)
        {
            var control = (CueControl) dependencyObject;
            var sampleLength = (uint) baseValue;
            return control.CoerceSampleLengthCallback(sampleLength);
        }

        private object CoerceSampleLengthCallback(uint sampleLength)
        {
            uint minSampleLength = ToSample(MinCueLength);
            if (sampleLength < minSampleLength)
                return minSampleLength;

            return sampleLength;
        }

        private uint ToSample(TimeSpan timeSpan)
        {
            /* Following is an unfortunate hack to get samples per snap interval. */
            var cue = DataContext as EventCueViewModel;
            if (cue == null)
                throw new InvalidOperationException("Expected DataContext to be EventCueViewModel");

            uint samples = cue.ToSample(timeSpan);

            return (uint) SampleDecimationConverter.Convert(samples, null, null, null);
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