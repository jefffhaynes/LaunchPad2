using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FMOD;
using LaunchPad2.Annotations;

namespace LaunchPad2.Controls
{
    public class CueTimeline : Canvas
    {
        private const int NaturalHeight = 24;

        public static readonly DependencyProperty TimeScaleProperty = DependencyProperty.Register(
            "TimeScale", typeof(double), typeof(CueTimeline),
            new PropertyMetadata(1.0, TimeScaleChangedCallback));

        public static readonly DependencyProperty AudioTrackProperty = DependencyProperty.Register(
            "AudioTrack", typeof(AudioTrack), typeof(CueTimeline), 
            new FrameworkPropertyMetadata(default(AudioTrack), 
                FrameworkPropertyMetadataOptions.AffectsArrange, 
                AudioTrackChangedCallback));


        public AudioTrack AudioTrack
        {
            get { return (AudioTrack)GetValue(AudioTrackProperty); }
            set { SetValue(AudioTrackProperty, value); }
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
            if (AudioTrack != null)
            {
                var length = AudioTrack.Length;
            }

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


        private static void AudioTrackChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var control = (CueTimeline)dependencyObject;
            control.AudioTrackChangedCallback(e);
        }

        protected virtual void TimeScaleChangedCallback(DependencyPropertyChangedEventArgs e)
        {

        }


        protected virtual void AudioTrackChangedCallback(DependencyPropertyChangedEventArgs e)
        {

        }
    }
}
