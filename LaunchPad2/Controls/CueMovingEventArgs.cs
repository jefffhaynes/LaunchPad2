using System.Windows;

namespace LaunchPad2.Controls
{
    public class CueMovingEventArgs : RoutedEventArgs
    {
        public CueMovingEventArgs(RoutedEvent routedEvent, CueMoveMode cueMoveMode)
            : base(routedEvent)
        {
            CueMoveMode = cueMoveMode;
        }

        public CueMoveMode CueMoveMode { get; set; }
    }
}
