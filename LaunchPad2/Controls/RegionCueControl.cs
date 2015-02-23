using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LaunchPad2.Controls
{
    public class RegionCueControl : CueControl
    {
        private ContentPresenter _contentPresenter;
        private Rect _rect;

        protected override bool HasGrips
        {
            get { return false; }
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            _rect = new Rect(0, 0,
                arrangeBounds.Width,
                arrangeBounds.Height);

            return arrangeBounds;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var leftPen = new Pen(BorderBrush, BorderThickness.Left);
            var topPen = new Pen(BorderBrush, BorderThickness.Top);
            var rightPen = new Pen(BorderBrush, BorderThickness.Right);
            var bottomPen = new Pen(BorderBrush, BorderThickness.Bottom);

            /* Draw cue */
            drawingContext.DrawRectangle(Background, null, _rect);

            /* Draw cue border */
            drawingContext.DrawLine(leftPen, _rect.TopLeft, _rect.BottomLeft);
            drawingContext.DrawLine(topPen, _rect.TopLeft, _rect.TopRight);
            drawingContext.DrawLine(rightPen, _rect.TopRight, _rect.BottomRight);
            drawingContext.DrawLine(bottomPen, _rect.BottomLeft, _rect.BottomRight);
        }

        //protected override double Snap(double value)
        //{
        //    return value;
        //}

        protected override void TimeScaleChangedCallback(DependencyPropertyChangedEventArgs e)
        {
            base.TimeScaleChangedCallback(e);

            double length = SampleLength * (double)e.NewValue;
            SetWidth(length);
        }


        protected override object CoerceSampleLengthCallback(int sampleLength)
        {
            return Math.Max(sampleLength, 0);
        }

        protected override void SetWidth(double width)
        {
            if (_contentPresenter == null)
                _contentPresenter = FindContentPresenter();

            SetValue(WidthProperty, width);
        }

        protected override Size GetNaturalSize()
        {
            return new Size(0, 0);
        }

        protected override double GetAutoscrollReferenceOffset()
        {
            return Sample*TimeScale;
        }

        internal ContentPresenter FindContentPresenter()
        {
            return FindAncestor<ContentPresenter>();
        }
    }
}