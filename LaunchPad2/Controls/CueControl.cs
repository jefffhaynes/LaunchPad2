using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LaunchPad2.Controls
{
    public abstract class CueControl : CueControlBase
    {
        private static readonly TimeSpan MinCueLength = TimeSpan.FromSeconds(0.5);

        public static readonly DependencyProperty GripWidthProperty = DependencyProperty.Register(
            "GripWidth", typeof (double), typeof (CueControl), new PropertyMetadata(0.0));

        private Canvas _canvas;
        private Point _canvasClickPosition;

        private double _clickLeft;
        private double _clickWidth;
        private CueMoveMode _cueMoveMode;

        protected CueControl()
        {
            MouseLeftButtonDown += OnLeftButtonDown;
            MouseMove += OnMouseMove;
            MouseLeftButtonUp += OnLeftButtonUp;
        }

        public double GripWidth
        {
            get { return (double) GetValue(GripWidthProperty); }
            set { SetValue(GripWidthProperty, value); }
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

        private async void AutoScroll()
        {
            while (IsMouseCaptured)
            {
                AutoScrollStep();
                await Task.Delay(10);
            }
        }

        private void OnLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            OnLeftButtonDownOverride(sender, e);
            e.Handled = true;
        }

        protected virtual void OnLeftButtonDownOverride(object sender, MouseButtonEventArgs e)
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
                else if (clickPosition.X > _clickWidth - GetGripWidth())
                    _cueMoveMode = CueMoveMode.RightGrip;
                else if (clickPosition.X < GetGripWidth())
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

                    int minSampleLength = ToSample(MinCueLength);
                    if (sampleLength < minSampleLength)
                        sampleLength = minSampleLength;

                    double snappedSampleLength = Snap(sampleLength);

                    SetValue(SampleLengthProperty, (int) snappedSampleLength);

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

        protected virtual double GetGripWidth()
        {
            return 0;
        }
    }
}