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
            Groups = new List<GroupModel>();
        }

        public Model(ViewModel viewModel) : this()
        {
            foreach(var device in viewModel.Devices)
                Devices.Add(new DeviceModel(device));

            foreach(var node in viewModel.Nodes)
                Nodes.Add(new NodeModel(node));

            foreach(var track in viewModel.Tracks)
                Tracks.Add(new TrackModel(track));

            /* We want to reference stored cue ids in stored groups */
            var cues = Tracks.SelectMany(track => track.Cues);
            var cueSources = cues.ToDictionary(cue => cue.Reference, cue => cue.Id);

            foreach(var group in viewModel.Groups)
                Groups.Add(new GroupModel(group, cueSources));
        }

        public ViewModel GetViewModel()
        {
            var devices = new ObservableCollection<DeviceViewModel>(Devices.Select(device => device.GetViewModel()));
            var nodes = new ObservableCollection<NodeViewModel>(Nodes.Select(node => node.GetViewModel()));
            var tracks = new ObservableCollection<TrackViewModel>(Tracks.Select(track => track.GetViewModel(devices, nodes)));

            var cues = Tracks.SelectMany(track => track.Cues);
            var cueSources = cues.ToDictionary(cue => cue.Id, cue => cue.Reference);

            var groups =
                new ObservableCollection<EventCueGroupViewModel>(
                    Groups.Select(group => group.GetViewModel(null, cueSources)));

            return new ViewModel
            {
                Devices = devices,
                Nodes = nodes,
                Tracks = tracks,
                Groups = groups
            };
        }

        public List<TrackModel> Tracks { get; set; }

        public List<DeviceModel> Devices { get; set; }

        public List<NodeModel> Nodes { get; set; }

        public List<GroupModel> Groups { get; set; } 
    }
}
