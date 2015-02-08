namespace LaunchPad2.ViewModels
{
    public class CueMoveInfo
    {
        public CueMoveInfo(EventCueViewModel cue, EventCueViewModel before)
        {
            Cue = cue;
            Before = before;
        }

        public EventCueViewModel Cue { get; private set; }

        public EventCueViewModel Before { get; private set; }

        public bool HasChanged
        {
            get { return Cue.Start != Before.Start || Cue.Length != Before.Length || Cue.LeadIn != Before.LeadIn; }
        }
    }
}
