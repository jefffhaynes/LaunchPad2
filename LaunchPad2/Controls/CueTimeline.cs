using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FMOD;
using LaunchPad2.Annotations;
using LaunchPad2.Properties;

namespace LaunchPad2.Controls
{
    public class CueTimeline : Canvas
    {
        public static readonly DependencyProperty TimeScaleProperty = DependencyProperty.Register(
            "TimeScale", typeof (double), typeof (CueTimeline),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty AudioTrackProperty = DependencyProperty.Register(
            "AudioTrack", typeof (AudioTrack), typeof (CueTimeline),
            new FrameworkPropertyMetadata(default(AudioTrack),
                FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty FontFamilyProperty = DependencyProperty.RegisterAttached(
            "FontFamily", typeof (FontFamily), typeof (CueTimeline),
            new FrameworkPropertyMetadata(SystemFonts.MessageFontFamily,
                FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.Inherits));

        public static readonly DependencyProperty FontStyleProperty = DependencyProperty.RegisterAttached(
            "FontStyle", typeof (FontStyle), typeof (CueTimeline),
            new FrameworkPropertyMetadata(SystemFonts.MessageFontStyle,
                FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.Inherits));

        public static readonly DependencyProperty FontWeightProperty = DependencyProperty.RegisterAttached(
            "FontWeight", typeof (FontWeight), typeof (CueTimeline),
            new FrameworkPropertyMetadata(SystemFonts.MessageFontWeight,
                FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.Inherits));

        public static readonly DependencyProperty FontStretchProperty = DependencyProperty.RegisterAttached(
            "FontStretch", typeof (FontStretch), typeof (CueTimeline),
            new FrameworkPropertyMetadata(FontStretches.Normal,
                FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.Inherits));

        public static readonly DependencyProperty FontSizeProperty = DependencyProperty.RegisterAttached(
            "FontSize", typeof (double), typeof (CueTimeline),
            new FrameworkPropertyMetadata(SystemFonts.MessageFontSize,
                FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.Inherits));

        public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register(
            "Foreground", typeof (Brush), typeof (CueTimeline),
            new FrameworkPropertyMetadata(default(Brush), FrameworkPropertyMetadataOptions.AffectsArrange));

        public static readonly DependencyProperty RegionStartProperty = DependencyProperty.Register(
            "RegionStart", typeof(TimeSpan), typeof(CueTimeline), new FrameworkPropertyMetadata(default(TimeSpan), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty RegionLengthProperty = DependencyProperty.Register(
            "RegionLength", typeof(TimeSpan), typeof(CueTimeline), new FrameworkPropertyMetadata(default(TimeSpan), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        private TimeSpan _labelMinorTickPeriod;
        private TimeSpan _labelPeriod;

        public TimeSpan RegionStart
        {
            get { return (TimeSpan) GetValue(RegionStartProperty); }
            set { SetValue(RegionStartProperty, value); }
        }

        public TimeSpan RegionLength
        {
            get { return (TimeSpan) GetValue(RegionLengthProperty); }
            set { SetValue(RegionLengthProperty, value); }
        }

        public double TimeScale
        {
            get { return (double) GetValue(TimeScaleProperty); }
            set { SetValue(TimeScaleProperty, value); }
        }

        public Brush Foreground
        {
            get { return (Brush) GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        public FontFamily FontFamily
        {
            get { return (FontFamily) GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }

        public FontStyle FontStyle
        {
            get { return (FontStyle) GetValue(FontStyleProperty); }
            set { SetValue(FontStyleProperty, value); }
        }

        public FontWeight FontWeight
        {
            get { return (FontWeight) GetValue(FontWeightProperty); }
            set { SetValue(FontWeightProperty, value); }
        }

        public FontStretch FontStretch
        {
            get { return (FontStretch) GetValue(FontStretchProperty); }
            set { SetValue(FontStretchProperty, value); }
        }

        public double FontSize
        {
            get { return (double) GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        public AudioTrack AudioTrack
        {
            get { return (AudioTrack) GetValue(AudioTrackProperty); }
            set { SetValue(AudioTrackProperty, value); }
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            if (AudioTrack != null)
            {
                TimeSpan length = AudioTrack.Length;
                Size labelSize = MeasureString("00:00");
                double naturalLabelCount = arrangeBounds.Width/(labelSize.Width*2);
                double naturalLabelPeriod = length.TotalMilliseconds/naturalLabelCount;

                if (naturalLabelPeriod < 1000) // 1 second
                {
                    _labelPeriod = TimeSpan.FromSeconds(1);
                    _labelMinorTickPeriod = TimeSpan.FromMilliseconds(200);
                }
                else if (naturalLabelPeriod < 2000) // 2 seconds
                {
                    _labelPeriod = TimeSpan.FromSeconds(2);
                    _labelMinorTickPeriod = TimeSpan.FromMilliseconds(400);
                }
                else if (naturalLabelPeriod < 5000) // 5 seconds
                {
                    _labelPeriod = TimeSpan.FromSeconds(5);
                    _labelMinorTickPeriod = TimeSpan.FromSeconds(1);
                }
                else if (naturalLabelPeriod < 10000) // 10 seconds
                {
                    _labelPeriod = TimeSpan.FromSeconds(10);
                    _labelMinorTickPeriod = TimeSpan.FromSeconds(1);
                }
                else if (naturalLabelPeriod < 30000) // 30 seconds
                {
                    _labelPeriod = TimeSpan.FromSeconds(30);
                    _labelMinorTickPeriod = TimeSpan.FromSeconds(5);
                }
                else if (naturalLabelPeriod < 60000) // 1 minute
                {
                    _labelPeriod = TimeSpan.FromMinutes(1);
                    _labelMinorTickPeriod = TimeSpan.FromSeconds(10);
                }
                else if (naturalLabelPeriod < 120000) // 2 minutes
                {
                    _labelPeriod = TimeSpan.FromMinutes(2);
                    _labelMinorTickPeriod = TimeSpan.FromSeconds(20);
                }
                else // 5 minutes
                {
                    _labelPeriod = TimeSpan.FromMinutes(5);
                    _labelMinorTickPeriod = TimeSpan.FromMinutes(1);
                }
            }

            return base.ArrangeOverride(arrangeBounds);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (AudioTrack != null && _labelPeriod != TimeSpan.Zero)
            {
                double tickCount = AudioTrack.Length.TotalMilliseconds/_labelPeriod.TotalMilliseconds;
                double tickStep = ToOffset(_labelPeriod);

                var pen = new Pen(Foreground, 1);
                const int tickHeight = 16;

                double minorTickStep = tickStep*_labelMinorTickPeriod.TotalMilliseconds/_labelPeriod.TotalMilliseconds;
                double minorTickCount = _labelPeriod.TotalMilliseconds/_labelMinorTickPeriod.TotalMilliseconds;

                const int minorTickHeight = 2;

                for (int i = 0; i < tickCount; i++)
                {
                    double x = tickStep*i;
                    drawingContext.DrawLine(pen, new Point(x, 0), new Point(x, tickHeight));

                    string label = TimeSpan.FromMilliseconds(_labelPeriod.TotalMilliseconds*i).ToString("mm\\:ss");
                    drawingContext.DrawText(GetFormattedText(label), new Point(x + 2, 0));

                    for (int j = 1; j < minorTickCount; j++)
                    {
                        double minorX = x + minorTickStep*j;
                        drawingContext.DrawLine(pen, new Point(minorX, 0), new Point(minorX, minorTickHeight));
                    }
                }
            }
        }

        private bool _selectingRegion;
        private Point _clickPosition;

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            CaptureMouse();
            _clickPosition = e.GetPosition(this);
            RegionLength = TimeSpan.Zero;
            _selectingRegion = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_selectingRegion)
            {
                var position = e.GetPosition(this);
                var delta = _clickPosition.X - position.X;
                var length = Math.Abs(delta);

                if (length > 2.0)
                {
                    RegionLength = ToTimeSpan(length);
                    RegionStart = delta < 0 ? ToTimeSpan(_clickPosition.X) : ToTimeSpan(_clickPosition.X - length);
                }
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            ReleaseMouseCapture();
            _selectingRegion = false;
        }

        private double ToOffset(TimeSpan value)
        {
            return value.TotalSeconds * AudioTrack.SampleRate * TimeScale * Settings.Default.Decimation;
        }

        private TimeSpan ToTimeSpan(double offset)
        {
            return TimeSpan.FromSeconds(offset/(AudioTrack.SampleRate*TimeScale*Settings.Default.Decimation));
        }

        private Size MeasureString(string value)
        {
            FormattedText formattedText = GetFormattedText(value);
            return new Size(formattedText.Width, formattedText.Height);
        }

        private FormattedText GetFormattedText(string value)
        {
            return new FormattedText(value, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                new Typeface(FontFamily, FontStyle, FontWeight, FontStretch), FontSize, Foreground);
        }
    }
}