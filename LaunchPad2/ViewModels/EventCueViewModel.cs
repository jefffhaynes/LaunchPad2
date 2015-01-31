using System;
using System.Collections.Generic;
using System.Linq;

namespace LaunchPad2.ViewModels
{
    public class EventCueViewModel : ViewModelBase, IGroupable
    {
        private bool _isActive;
        private TimeSpan _leadIn;
        private TimeSpan _length;
        private string _notes;
        private float _sampleRate;
        private TimeSpan _start;

        public EventCueViewModel(float sampleRate)
        {
            SampleRate = sampleRate;
        }

        public EventCueViewModel(float sampleRate, TimeSpan start, TimeSpan length) : this(sampleRate)
        {
            Start = start;
            Length = length;
        }

        public EventCueViewModel(float sampleRate, TimeSpan start, TimeSpan length, TimeSpan leadIn) : 
            this(sampleRate, start, length)
        {
            LeadIn = leadIn;
        }

        public TimeSpan Start
        {
            get { return _start; }
            set
            {
                if (value == _start)
                    return;

                _start = value;
                OnPropertyChanged();
                OnPropertyChanged("StartSample");
            }
        }

        public TimeSpan Length
        {
            get { return _length; }
            set
            {
                if (value == _length)
                    return;

                _length = value;
                OnPropertyChanged();
                OnPropertyChanged("SampleLength");
            }
        }

        public TimeSpan LeadIn
        {
            get { return _leadIn; }
            set
            {
                if (value == _leadIn)
                    return;

                _leadIn = value;
                OnPropertyChanged();
                OnPropertyChanged("LeadInSampleLength");
            }
        }

        public TimeSpan End
        {
            get { return Start + Length; }
            set { Start = value - Length; }
        }

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
        }

        private bool _isLockedToDevice;

        public bool IsLockedToDevice
        {
            get { return _isLockedToDevice; }
            set
            {
                if (_isLockedToDevice != value)
                {
                    _isLockedToDevice = value;
                    OnPropertyChanged();
                }
            }
        }

        public float SampleRate
        {
            get { return _sampleRate; }

            set
            {
                if (Math.Abs(_sampleRate - value) > float.Epsilon)
                {
                    _sampleRate = value;
                    OnPropertyChanged();
                    OnPropertyChanged("StartSample");
                    OnPropertyChanged("SampleLength");
                    OnPropertyChanged("LeadInSampleLength");
                }
            }
        }

        public uint StartSample
        {
            get { return ToSample(Start); }

            set
            {
                Start = FromSample(value);
                OnPropertyChanged();
            }
        }

        public uint SampleLength
        {
            get { return ToSample(Length); }

            set
            {
                Length = FromSample(value);
                OnPropertyChanged();
            }
        }

        public uint LeadInSampleLength
        {
            get { return ToSample(LeadIn); }

            set
            {
                LeadIn = FromSample(value);
                OnPropertyChanged();
            }
        }

        public uint EndSample
        {
            get { return ToSample(End); }
            set { End = FromSample(value); }
        }

        private bool _isSelected;

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;

                    if (Group != null)
                    {
                        var root = GetRootGroupable();
                        var descendants = root.GetDescendants().OfType<EventCueViewModel>();

                        foreach (var descendant in descendants)
                            descendant.IsSelected = value;
                    }

                    OnPropertyChanged();
                }
            }
        }

        public bool IsActive
        {
            get { return _isActive; }
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Notes
        {
            get { return _notes; }
            set
            {
                if (_notes != value)
                {
                    _notes = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool Intersects(TimeSpan time)
        {
            if (time < Start - LeadIn)
                return false;

            return time <= End;
        }

        public bool Intersects(EventCueViewModel cue)
        {
            if(cue.End < Start - LeadIn)
                return false;

            return cue.Start <= End;
        }

        public uint ToSample(TimeSpan time)
        {
            double totalSeconds = time.TotalMilliseconds/1000;
            return (uint) (totalSeconds*SampleRate);
        }

        public TimeSpan FromSample(uint samples)
        {
            return TimeSpan.FromMilliseconds((double)samples*1000/(SampleRate));
        }

        public EventCueViewModel Clone()
        {
            return new EventCueViewModel(SampleRate, Start, Length, LeadIn);
        }
    }
}