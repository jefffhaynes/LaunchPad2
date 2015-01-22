using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using LaunchPad2.ViewModels;

namespace LaunchPad2.Models
{
    [Serializable]
    public class GroupModel : ISerializable
    {
        private const string GroupsSerializationInfoKey = "groups";
        private const string CuesSerializationInfoKey = "cues";

        public GroupModel()
        {
        }

        public GroupModel(EventCueGroupViewModel groupViewModel, IDictionary<EventCueViewModel, string> cueSources)
        {
            var childGroups = groupViewModel.Children.OfType<EventCueGroupViewModel>();
            var childCues = groupViewModel.Children.OfType<EventCueViewModel>();

            Groups = new List<GroupModel>(childGroups.Select(childGroup => new GroupModel(childGroup, cueSources)));
            CueIds = new List<string>(childCues.Select(childCue => cueSources[childCue]));
        }

        public GroupModel(SerializationInfo info, StreamingContext context)
        {
            Groups = (List<GroupModel>) info.GetValue(GroupsSerializationInfoKey, typeof (List<GroupModel>));
            CueIds = (List<string>) info.GetValue(CuesSerializationInfoKey, typeof (List<string>));
        }

        public List<GroupModel> Groups { get; set; }

        public List<string> CueIds { get; set; }

        public EventCueGroupViewModel GetViewModel(EventCueGroupViewModel parent, IDictionary<string, EventCueViewModel> cueSources)
        {
            var groupViewModel = new EventCueGroupViewModel { Group = parent };
            var groups = Groups.Select(group => group.GetViewModel(groupViewModel, cueSources));
            var cues = CueIds.Select(cueId => cueSources[cueId]);
            var groupables = groups.Cast<IGroupable>().Union(cues).ToList();

            foreach (var group in groupables)
                group.Group = groupViewModel;

            groupViewModel.Children = new ObservableCollection<IGroupable>(groupables);

            return groupViewModel;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(GroupsSerializationInfoKey, Groups);
            info.AddValue(CuesSerializationInfoKey, CueIds);
        }
    }
}
