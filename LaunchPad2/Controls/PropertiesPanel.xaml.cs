using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace LaunchPad2.Controls
{
    public partial class PropertiesPanel : UserControl
    {
        public static readonly DependencyProperty DeviceSourceProperty = DependencyProperty.Register(
            "DeviceSource", typeof (IList), typeof (PropertiesPanel), new PropertyMetadata(default(IList)));

        public static readonly DependencyProperty NodeSourceProperty = DependencyProperty.Register(
            "NodeSource", typeof (IList), typeof (PropertiesPanel), new PropertyMetadata(default(IList)));

        public PropertiesPanel()
        {
            InitializeComponent();
        }

        public IList DeviceSource
        {
            get { return (IList) GetValue(DeviceSourceProperty); }
            set { SetValue(DeviceSourceProperty, value); }
        }

        public IList NodeSource
        {
            get { return (IList) GetValue(NodeSourceProperty); }
            set { SetValue(NodeSourceProperty, value); }
        }
    }
}