﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace LaunchPad2.ViewModels
{
    public class EventCueViewModel : ViewModelBase, IGroupable
    {
        private bool _isActive;
        private bool _isLockedToDevice;
        private bool _isSelected;
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
// ReSharper disable ExplicitCallerInfoArgument
                OnPropertyChanged("StartSample");
// ReSharper restore ExplicitCallerInfoArgument
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
// ReSharper disable ExplicitCallerInfoArgument
                OnPropertyChanged("SampleLength");
// ReSharper restore ExplicitCallerInfoArgument
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
// ReSharper disable ExplicitCallerInfoArgument
                OnPropertyChanged("LeadInSampleLength");
// ReSharper restore ExplicitCallerInfoArgument
            }
        }

        public TimeSpan End
        {
            get { return Start + Length; }
            set { Start = value - Length; }
        }

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
// ReSharper disable ExplicitCallerInfoArgument
                    OnPropertyChanged("StartSample");
                    OnPropertyChanged("SampleLength");
                    OnPropertyChanged("LeadInSampleLength");
// ReSharper restore ExplicitCallerInfoArgument
                }
            }
        }

        public uint StartSample
        {
            get { return (uint)ToSample(Start); }

            set
            {
                Start = FromSample(value);
                OnPropertyChanged();
            }
        }

        public int SampleLength
        {
            get { return (int)ToSample(Length); }

            set
            {
                Length = FromSample(value);
                OnPropertyChanged();
            }
        }

        public uint LeadInSampleLength
        {
            get { return (uint)ToSample(LeadIn); }

            set
            {
                LeadIn = FromSample(value);
                OnPropertyChanged();
            }
        }

        public uint EndSample
        {
            get { return (uint)ToSample(End); }
            set { End = FromSample(value); }
        }

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
                        IGroupable root = GetRootGroupable();
                        IEnumerable<EventCueViewModel> descendants = root.GetDescendants().OfType<EventCueViewModel>();

                        foreach (EventCueViewModel descendant in descendants)
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

        public void Select()
        {
            IsSelected = true;
        }

        public void Unselect()
        {
            IsSelected = false;
        }

        public bool Intersects(TimeSpan time)
        {
            if (time < Start - LeadIn)
                return false;

            return time <= End;
        }

        public bool Intersects(EventCueViewModel cue)
        {
            if (cue.End < Start - LeadIn)
                return false;

            return cue.Start <= End;
        }

        public double ToSample(TimeSpan time)
        {
            double totalSeconds = time.TotalMilliseconds/1000;
            return (int) (totalSeconds*SampleRate);
        }

        public TimeSpan FromSample(double samples)
        {
            return TimeSpan.FromMilliseconds(samples*1000/(SampleRate));
        }

        public EventCueViewModel Clone()
        {
            return new EventCueViewModel(SampleRate, Start, Length, LeadIn);
        }
    }
}