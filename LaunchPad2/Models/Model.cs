using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using LaunchPad2.ViewModels;

namespace LaunchPad2.Models
{
    public class Model
    {
        public Model()
        {
            Tracks = new List<TrackModel>();
            Devices = new List<DeviceModel>();
            Nodes = new List<NodeModel>();
        }

        public Model(ViewModel viewModel) : this()
        {
            foreach(var device in viewModel.Devices)
                Devices.Add(new DeviceModel(device));

            foreach(var node in viewModel.Nodes)
                Nodes.Add(new NodeModel(node));

            foreach(var track in viewModel.Tracks)
                Tracks.Add(new TrackModel(track));
        }

        public ViewModel GetViewModel()
        {
            var devices = new ObservableCollection<DeviceViewModel>(Devices.Select(device => device.GetViewModel()));
            var nodes = new ObservableCollection<NodeViewModel>(Nodes.Select(node => node.GetViewModel()));

            return new ViewModel
            {
                Devices = devices,
                Nodes = nodes,
                Tracks = new ObservableCollection<TrackViewModel>(Tracks.Select(track => track.GetViewModel(devices, nodes)))
            };
        }

        public List<TrackModel> Tracks { get; set; }

        public List<DeviceModel> Devices { get; set; }

        public List<NodeModel> Nodes { get; set; } 
    }
}
