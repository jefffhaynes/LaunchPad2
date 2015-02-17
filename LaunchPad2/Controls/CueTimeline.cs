using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LaunchPad2.Controls
{
    public class CueTimeline : Canvas
    {
        private const int NaturalHeight = 24;

        public static readonly DependencyProperty TimeScaleProperty = DependencyProperty.Register(
            "TimeScale", typeof(double), typeof(CueTimeline),
            new PropertyMetadata(1.0, TimeScaleChangedCallback));

        public static readonly DependencyProperty SampleRateProperty = DependencyProperty.Register(
            "SampleRate", typeof (double), typeof (CueTimeline), new PropertyMetadata(default(double)));

        public double SampleRate
        {
            get { return (double) GetValue(SampleRateProperty); }
            set { SetValue(SampleRateProperty, value); }
        }

        static CueTimeline()
        {
            //DefaultStyleKeyProperty.OverrideMetadata(typeof(CueTimeline), new FrameworkPropertyMetadata(typeof(CueTimeline)));
        }

        protected override Size MeasureOverride(Size constraint)
        {
            double width = constraint.Width;
            double height = constraint.Height;

            var naturalSize = new Size(0, NaturalHeight);

            if (double.IsInfinity(width) && double.IsInfinity(height))
                return naturalSize;

            if (double.IsInfinity(width))
                return new Size(naturalSize.Width, height);

            if (double.IsInfinity(height))
                return new Size(width, naturalSize.Height);

            width = Math.Max(width, naturalSize.Width);
            height = Math.Max(height, naturalSize.Height);

            return new Size(width, height);
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            return arrangeBounds;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
        }

        private static void TimeScaleChangedCallback(DependencyObject dependencyObject,
         DependencyPropertyChangedEventArgs e)
        {
            var control = (CueTimeline)dependencyObject;
            control.TimeScaleChangedCallback(e);
        }

        protected virtual void TimeScaleChangedCallback(DependencyPropertyChangedEventArgs e)
        {

        }
    }
}
