using System;

namespace LaunchPad2.ViewModels
{
    public class RegionInfo<TValue>
    {
        public RegionInfo(TimeSpan start, TimeSpan length, TValue value)
        {
            Start = start;
            Length = length;
            Value = value;
        }
 
        public TimeSpan Start { get; private set; }

        public TimeSpan Length { get; private set; }

        public TValue Value { get; private set; }
    }
}
