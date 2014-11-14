using System;

namespace LaunchPad2.ViewModels
{
    public class SampleInfo<TValue>
    {
        public SampleInfo(TimeSpan time, TValue value)
        {
            Time = time;
            Value = value;
        }

        public TimeSpan Time { get; private set; }
        public TValue Value { get; private set; }
    }
}
