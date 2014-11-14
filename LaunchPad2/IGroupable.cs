using System.Collections.Generic;
using LaunchPad2.ViewModels;

namespace LaunchPad2
{
    public interface IGroupable
    {
        EventCueGroupViewModel Group { get; set; }

        IGroupable GetRootGroupable();

        IEnumerable<IGroupable> GetDescendants();
    }
}
