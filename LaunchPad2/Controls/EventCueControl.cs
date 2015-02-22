using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LaunchPad2.Controls
{
    public class EventCueControl : CueControl
    {
        private const double MinGripWidth = 4.0;

        public static readonly DependencyProperty GripBrushProperty = DependencyProperty.Register(
            "GripBrush", typeof (Brush), typeof (EventCueControl),
            new PropertyMetadata(new SolidColorBrush(Colors.LightSlateGray)));

        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
            "IsSelected", typeof(bool), typeof(EventCueControl), new FrameworkPropertyMetadata(default(bool), 
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty LeadInSampleLengthProperty = DependencyProperty.Register(
            "LeadInSampleLength", typeof (uint), typeof (EventCueControl), new FrameworkPropertyMetadata(default(uint),
                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty LeadInBarWidthProperty = DependencyProperty.Register(
            "LeadInBarWidth", typeof (double), typeof (EventCueControl), new PropertyMetadata(4.0));

        public static readonly DependencyProperty LeadInBrushProperty = DependencyProperty.Register(
            "LeadInBrush", typeof (Brush), typeof (EventCueControl),
            new FrameworkPropertyMetadata(new SolidColorBrush(Colors.Gray), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty LeadInShownProperty = DependencyProperty.Register(
            "LeadInShown", typeof (bool), typeof (EventCueControl), new FrameworkPropertyMetadata(true,
                FrameworkPropertyMetadataOptions.AffectsRender));

        private ContentPresenter _contentPresenter;

        private Rect _leftGrip = Rect.Empty;
        private Rect _rect = Rect.Empty;
        private Rect _rightGrip = Rect.Empty;

        static EventCueControl()
        {
            BorderBrushProperty.OverrideMetadata(typeof (EventCueControl),
                new FrameworkPropertyMetadata(default(Brush), FrameworkPropertyMetadataOptions.AffectsRender));

            BackgroundProperty.OverrideMetadata(typeof (EventCueControl),
                new FrameworkPropertyMetadata(default(Brush), FrameworkPropertyMetadataOptions.AffectsRender));

            TimeScaleProperty.OverrideMetadata(typeof (EventCueControl),
                new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender | 
                    FrameworkPropertyMetadataOptions.AffectsMeasure | 
                    FrameworkPropertyMetadataOptions.AffectsArrange));
        }

        public bool LeadInShown
        {
            get { return (bool) GetValue(LeadInShownProperty); }
            set { SetValue(LeadInShownProperty, value); }
        }

        public Brush LeadInBrush
        {
            get { return (Brush) GetValue(LeadInBrushProperty); }
            set { SetValue(LeadInBrushProperty, value); }
        }

        public double LeadInBarWidth
        {
            get { return (double) GetValue(LeadInBarWidthProperty); }
            set { SetValue(LeadInBarWidthProperty, value); }
        }

        public uint LeadInSampleLength
        {
            get { return (uint) GetValue(LeadInSampleLengthProperty); }
            set { SetValue(LeadInSampleLengthProperty, value); }
        }

        public bool IsSelected
        {
            get { return (bool) GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        public Brush GripBrush
        {
            get { return (Brush) GetValue(GripBrushProperty); }
            set { SetValue(GripBrushProperty, value); }
        }

        protected override bool HasGrips
        {
            get { return true; }
        }

        protected override void OnLeftButtonDownProtected(object sender, MouseButtonEventArgs e)
        {
            if (!IsSelected)
            {
                var routedEventArgs = new RoutedEventArgs(SelectionCanvas.CueSelectedEvent);
                RaiseEvent(routedEventArgs);
            }
            e.Handled = true;

            base.OnLeftButtonDownProtected(sender, e);
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            double leftInset = BorderThickness.Left/2.0;
            double topInset = BorderThickness.Top/2.0;
            double rightInset = BorderThickness.Right/2.0;
            double bottomInset = BorderThickness.Bottom/2.0;

            _rect = new Rect(leftInset, topInset,
                arrangeBounds.Width - leftInset - rightInset,
                arrangeBounds.Height - topInset - bottomInset);

            var gripWidth = GetGripWidth();

            if (gripWidth > MinGripWidth)
            {
                _leftGrip = new Rect(_rect.Left + 1, _rect.Top + 1,
                    gripWidth - 2, _rect.Height - 2);

                _rightGrip = new Rect(_rect.Right - gripWidth + 1, _rect.Top + 1,
                    gripWidth - 2, _rect.Height - 2);
            }
            else
            {
                _leftGrip = Rect.Empty;
                _rightGrip = Rect.Empty;
            }

            return arrangeBounds;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var leftPen = new Pen(BorderBrush, BorderThickness.Left);
            var topPen = new Pen(BorderBrush, BorderThickness.Top);
            var rightPen = new Pen(BorderBrush, BorderThickness.Right);
            var bottomPen = new Pen(BorderBrush, BorderThickness.Bottom);
            var leadInPen = new Pen(LeadInBrush, 1.0);

            /* Draw cue */
            drawingContext.DrawRectangle(Background, null, _rect);

            /* Draw cue border */
            drawingContext.DrawLine(leftPen, _rect.TopLeft, _rect.BottomLeft);
            drawingContext.DrawLine(topPen, _rect.TopLeft, _rect.TopRight);
            drawingContext.DrawLine(rightPen, _rect.TopRight, _rect.BottomRight);
            drawingContext.DrawLine(bottomPen, _rect.BottomLeft, _rect.BottomRight);

            /* Draw grips */
            drawingContext.DrawRectangle(GripBrush, null, _leftGrip);
            drawingContext.DrawRectangle(GripBrush, null, _rightGrip);

            /* Draw lead-in bar */
            if (LeadInShown)
            {
                double leadInLength = LeadInSampleLength*TimeScale;
                drawingContext.DrawRectangle(LeadInBrush, null,
                    new Rect(-(leadInLength + LeadInBarWidth), _rect.Top + 1, LeadInBarWidth, _rect.Height - 2));
                drawingContext.DrawLine(leadInPen,
                    new Point(-leadInLength, _rect.Height/2),
                    new Point(0, _rect.Height/2));
            }
        }

        protected override void TimeScaleChangedCallback(DependencyPropertyChangedEventArgs e)
        {
            base.TimeScaleChangedCallback(e);

            double length = SampleLength*(double) e.NewValue;
            SetWidth(length);
        }

        protected override void SetLeft(double left)
        {
            Offset = left;

            if (_contentPresenter == null)
                _contentPresenter = FindContentPresenter();

            _contentPresenter.SetValue(Canvas.LeftProperty, left);
        }

        protected override double GetLeft()
        {
            if (_contentPresenter == null)
                _contentPresenter = FindContentPresenter();

            return (double) _contentPresenter.GetValue(Canvas.LeftProperty);
        }

        protected override void SetWidth(double width)
        {
            if (_contentPresenter == null)
                _contentPresenter = FindContentPresenter();

            _contentPresenter.SetValue(WidthProperty, width);
        }

        protected override double GetWidth()
        {
            if (_contentPresenter == null)
                _contentPresenter = FindContentPresenter();

            return (double) _contentPresenter.GetValue(WidthProperty);
        }

        protected override double GetGripWidth()
        {
            return Math.Min(GripWidth, _rect.Width / 3);
        }

        protected override Size GetNaturalSize()
        {
            return new Size(2, 0);
        }

        internal ContentPresenter FindContentPresenter()
        {
            return FindAncestor<ContentPresenter>();
        }
    }
}