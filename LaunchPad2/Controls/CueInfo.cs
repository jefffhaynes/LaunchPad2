namespace LaunchPad2.Controls
{
    public class CueInfo
    {
        public CueInfo(double position, double length, double leadInLength)
        {
            Position = position;
            Length = length;
            LeadInLength = leadInLength;
        }

        public double Position { get; set; }

        public double Length { get; set; }

        public double LeadInLength { get; set; }
    }
}
