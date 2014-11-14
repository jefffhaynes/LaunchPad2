using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace LaunchPad2.ViewModels
{
    public class EventCueGroupViewModel : IGroupable
    {
        public EventCueGroupViewModel()
        {
            Children = new ObservableCollection<IGroupable>();
        }

        public ObservableCollection<IGroupable> Children { get; set; }

        public EventCueGroupViewModel Group { get; set; }

        public IGroupable GetRootGroupable()
        {
            IGroupable root = this;

            while (root.Group != null)
                root = root.Group;

            return root;
        }

        public IEnumerable<IGroupable> GetDescendants()
        {
            yield return this;

            foreach (var item in Children.SelectMany(child => child.GetDescendants()))
                yield return item;
        }
    }
}
