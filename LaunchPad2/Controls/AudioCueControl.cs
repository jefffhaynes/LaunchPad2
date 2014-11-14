using System;
using System.Windows;
using System.Windows.Media;

namespace LaunchPad2.Controls
{
    public class AudioCueControl : CueControl
    {
        public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
            "StrokeThickness", typeof (double), typeof (AudioCueControl), new FrameworkPropertyMetadata(default(double),
                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
            "Stroke", typeof (Brush), typeof (AudioCueControl), new FrameworkPropertyMetadata(default(Brush),
                FrameworkPropertyMetadataOptions.AffectsRender));

        private double _height;

        public Brush Stroke
        {
            get { return (Brush) GetValue(StrokeProperty); }
            set { SetValue(StrokeProperty, value); }
        }

        public double StrokeThickness
        {
            get { return (double) GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }

        protected override bool HasGrips
        {
            get { return false; }
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            _height = Math.Max(0.0, arrangeBounds.Height);
            return arrangeBounds;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var pen = new Pen(Stroke, StrokeThickness);
            double x = -StrokeThickness/2.0;
            drawingContext.DrawLine(pen, new Point(x, 0), new Point(x, _height));
        }

        protected override double Snap(double value)
        {
            return value;
        }

        protected override Size GetNaturalSize()
        {
            return new Size(StrokeThickness, 0);
        }

        protected override double GetAutoscrollReferenceOffset()
        {
            return Sample*TimeScale;
        }
    }
}