namespace LaunchPad2.ViewModels
{
    public class CueMoveInfo
    {
        public CueMoveInfo(EventCueViewModel cue, EventCueViewModel before)
        {
            Cue = cue;
            Before = before;
        }

        public EventCueViewModel Cue { get; }

        public EventCueViewModel Before { get; }

        public bool HasChanged => 
            Cue.Start != Before.Start || Cue.Length != Before.Length || Cue.LeadIn != Before.LeadIn;
    }
}
