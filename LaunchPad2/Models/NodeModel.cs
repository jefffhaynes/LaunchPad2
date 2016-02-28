using System.Xml.Serialization;
using LaunchPad2.ViewModels;
using XBee;

namespace LaunchPad2.Models
{
    public class NodeModel
    {
        public NodeModel()
        {
        }

        public NodeModel(NodeViewModel viewModel)
        {
            Name = viewModel.Name;
            Address = viewModel.Address.Value;
            IsEnabled = viewModel.IsEnabled;
            Notes = viewModel.Notes;
        }

        public NodeViewModel GetViewModel()
        {
            return new NodeViewModel(Name, new LongAddress(Address))
            {
                IsEnabled = IsEnabled,
                Notes = Notes
            };
        }

        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public ulong Address { get; set; }

        [XmlAttribute]
        public bool IsEnabled { get; set; }

        [XmlAttribute]
        public string Notes { get; set; }
    }
}
