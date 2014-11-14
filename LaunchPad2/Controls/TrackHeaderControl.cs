using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LaunchPad2.Controls
{
    public class TrackHeaderControl : ContentControl
    {
        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
            "IsSelected", typeof(bool), typeof(TrackHeaderControl), new PropertyMetadata(default(bool)));

        public bool IsSelected
        {
            get { return (bool) GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        private void TrackHeaderControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.Focus(this);
            var routedEventArgs = new RoutedEventArgs(SelectionCanvas.TrackSelectedEvent);
            RaiseEvent(routedEventArgs);
            e.Handled = true;
        }

        public TrackHeaderControl()
        {
            Loaded += TrackHeaderControl_Loaded;
            MouseLeftButtonDown += TrackHeaderControl_MouseLeftButtonDown;
            MouseLeftButtonUp += TrackHeaderControl_MouseLeftButtonUp;
            SetValue(Panel.ZIndexProperty, int.MaxValue);
        }

        void TrackHeaderControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        void TrackHeaderControl_Loaded(object sender, RoutedEventArgs e)
        {
            var scrollViewer = UiHelper.FindAncestor<ScrollViewer>(this);
            scrollViewer.ScrollChanged += ScrollViewerScrollChanged;
        }

        void ScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            SetValue(Canvas.LeftProperty, e.HorizontalOffset);
        }
    }
}
