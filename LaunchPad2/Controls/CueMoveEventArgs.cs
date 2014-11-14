using System.Windows;

namespace LaunchPad2.Controls
{
    public class CueMoveEventArgs : RoutedEventArgs
    {
        public CueMoveEventArgs(RoutedEvent routedEvent, double delta) : base(routedEvent)
        {
            Delta = delta;
        }


        public double Delta { get; set; }
    }
}
