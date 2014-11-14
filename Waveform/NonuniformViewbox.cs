
using System;
using System.Runtime;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Waveform
{
    public class NonuniformViewbox : Decorator
    {
        public static readonly DependencyProperty StretchOrientationProperty = DependencyProperty.Register(
            "StretchOrientation", typeof (StretchOrientation), typeof (NonuniformViewbox), 
            new FrameworkPropertyMetadata(StretchOrientation.Both, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public StretchOrientation StretchOrientation
        {
            get { return (StretchOrientation) GetValue(StretchOrientationProperty); }
            set { SetValue(StretchOrientationProperty, value); }
        }

        public static readonly DependencyProperty StretchProperty = DependencyProperty.Register("Stretch",
            typeof (Stretch), typeof (NonuniformViewbox),
            new FrameworkPropertyMetadata(Stretch.Uniform, FrameworkPropertyMetadataOptions.AffectsMeasure),
            ValidateStretchValue);

        public static readonly DependencyProperty StretchDirectionProperty =
            DependencyProperty.Register("StretchDirection", typeof (StretchDirection), typeof (NonuniformViewbox),
                new FrameworkPropertyMetadata(StretchDirection.Both, FrameworkPropertyMetadataOptions.AffectsMeasure),
                ValidateStretchDirectionValue);

        private ContainerVisual _internalVisual;

        static NonuniformViewbox()
        {
        }

        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public NonuniformViewbox()
        {
        }

        private ContainerVisual InternalVisual
        {
            get
            {
                if (_internalVisual == null)
                {
                    _internalVisual = new ContainerVisual();
                    AddVisualChild(_internalVisual);
                }
                return _internalVisual;
            }
        }

        private UIElement InternalChild
        {
            get
            {
                VisualCollection children = InternalVisual.Children;
                if (children.Count != 0)
                    return children[0] as UIElement;
                return null;
            }
            set
            {
                VisualCollection children = InternalVisual.Children;
                if (children.Count != 0)
                    children.Clear();
                children.Add(value);
            }
        }

        private Transform InternalTransform
        {
            get { return InternalVisual.Transform; }
            set { InternalVisual.Transform = value; }
        }

        public override UIElement Child
        {
            [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")] get { return InternalChild; }
            set
            {
                UIElement internalChild = InternalChild;
                if (internalChild == value)
                    return;
                RemoveLogicalChild(internalChild);
                if (value != null)
                    AddLogicalChild(value);
                InternalChild = value;
                InvalidateMeasure();
            }
        }

        protected override int VisualChildrenCount
        {
            get { return 1; }
        }

        public Stretch Stretch
        {
            get { return (Stretch) GetValue(Viewbox.StretchProperty); }
            set { SetValue(Viewbox.StretchProperty, value); }
        }

        public StretchDirection StretchDirection
        {
            get { return (StretchDirection) GetValue(Viewbox.StretchDirectionProperty); }
            set { SetValue(Viewbox.StretchDirectionProperty, value); }
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index != 0)
                throw new ArgumentOutOfRangeException("index");
            return InternalVisual;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            UIElement internalChild = InternalChild;
            var size = new Size();
            if (internalChild != null)
            {
                var availableSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
                internalChild.Measure(availableSize);
                Size desiredSize = internalChild.DesiredSize;
                Size scaleFactor = ComputeScaleFactor(constraint, desiredSize, Stretch, StretchDirection);

                var widthScale = (StretchOrientation == StretchOrientation.Both ||
                                  StretchOrientation == StretchOrientation.Horizontal)
                    ? scaleFactor.Width
                    : 1.0;

                var heightScale = (StretchOrientation == StretchOrientation.Both ||
                                   StretchOrientation == StretchOrientation.Vertical)
                    ? scaleFactor.Height
                    : 1.0;

                size.Width = widthScale*desiredSize.Width;
                size.Height = heightScale*desiredSize.Height;
            }
            return size;
        }


        protected override Size ArrangeOverride(Size arrangeSize)
        {
            UIElement internalChild = InternalChild;
            if (internalChild != null)
            {
                Size desiredSize = internalChild.DesiredSize;
                Size scaleFactor = ComputeScaleFactor(arrangeSize, desiredSize, Stretch, StretchDirection);

                var widthScale = (StretchOrientation == StretchOrientation.Both ||
                                  StretchOrientation == StretchOrientation.Horizontal)
                    ? scaleFactor.Width
                    : 1.0;

                var heightScale = (StretchOrientation == StretchOrientation.Both ||
                                   StretchOrientation == StretchOrientation.Vertical)
                    ? scaleFactor.Height
                    : 1.0;

                InternalTransform = new ScaleTransform(widthScale, heightScale);
                internalChild.Arrange(new Rect(new Point(), internalChild.DesiredSize));

                arrangeSize.Width = widthScale * desiredSize.Width;
                arrangeSize.Height = heightScale*desiredSize.Height;
            }
            return arrangeSize;
        }

        internal static Size ComputeScaleFactor(Size availableSize, Size contentSize, Stretch stretch,
            StretchDirection stretchDirection)
        {
            double width = 1.0;
            double height = 1.0;
            bool flag1 = !double.IsPositiveInfinity(availableSize.Width);
            bool flag2 = !double.IsPositiveInfinity(availableSize.Height);
            if ((stretch == Stretch.Uniform || stretch == Stretch.UniformToFill || stretch == Stretch.Fill) &&
                (flag1 || flag2))
            {
                width = Math.Abs(contentSize.Width) < double.Epsilon ? 0.0 : availableSize.Width/contentSize.Width;
                height = Math.Abs(contentSize.Height) < double.Epsilon ? 0.0 : availableSize.Height/contentSize.Height;
                if (!flag1)
                    width = height;
                else if (!flag2)
                {
                    height = width;
                }
                else
                {
                    switch (stretch)
                    {
                        case Stretch.Uniform:
                            double num1 = width < height ? width : height;
                            width = height = num1;
                            break;
                        case Stretch.UniformToFill:
                            double num2 = width > height ? width : height;
                            width = height = num2;
                            break;
                    }
                }
                switch (stretchDirection)
                {
                    case StretchDirection.UpOnly:
                        if (width < 1.0)
                            width = 1.0;
                        if (height < 1.0)
                        {
                            height = 1.0;
                        }
                        break;
                    case StretchDirection.DownOnly:
                        if (width > 1.0)
                            width = 1.0;
                        if (height > 1.0)
                        {
                            height = 1.0;
                        }
                        break;
                }
            }

            return new Size(width, height);
        }

        private static bool ValidateStretchValue(object value)
        {
            var stretch = (Stretch) value;
            switch (stretch)
            {
                case Stretch.Uniform:
                case Stretch.None:
                case Stretch.Fill:
                    return true;
                default:
                    return stretch == Stretch.UniformToFill;
            }
        }

        private static bool ValidateStretchDirectionValue(object value)
        {
            var stretchDirection = (StretchDirection) value;
            switch (stretchDirection)
            {
                case StretchDirection.Both:
                case StretchDirection.DownOnly:
                    return true;
                default:
                    return stretchDirection == StretchDirection.UpOnly;
            }
        }
    }
}