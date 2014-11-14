using System.Windows.Media;

namespace LaunchPad2.ViewModels
{
    public class BrushViewModel
    {
        public BrushViewModel(Brush brush)
        {
            Brush = brush;
        }

        public Brush Brush { get; private set; }

        public string Key
        {
            get { return Brush.ToString(); }
        }
    }
}
