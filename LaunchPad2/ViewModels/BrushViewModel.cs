using System.Windows.Media;

namespace LaunchPad2.ViewModels
{
    public class BrushViewModel
    {
        public BrushViewModel(Brush brush)
        {
            Brush = brush;
        }

        public Brush Brush { get; }

        public string Key => Brush.ToString();
    }
}
