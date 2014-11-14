using System;

namespace LaunchPad2.ViewModels
{
    public class DeviceViewModel : ViewModelBase
    {
        private TimeSpan _leadIn;
        private TimeSpan _length;
        private string _name;
        private string _notes;

        public DeviceViewModel()
        {
            Id = Guid.NewGuid().ToString();
            Length = TimeSpan.FromSeconds(1);
        }

        public string Id { get; set; }

        public string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        public TimeSpan LeadIn
        {
            get { return _leadIn; }
            set
            {
                if (_leadIn != value)
                {
                    _leadIn = value;
                    OnPropertyChanged();
                }
            }
        }

        public TimeSpan Length
        {
            get { return _length; }
            set
            {
                if (_length != value)
                {
                    _length = value;
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

        public DeviceViewModel Clone()
        {
            return new DeviceViewModel
            {
                Id = Id,
                Name = Name,
                LeadIn = LeadIn,
                Length = Length,
                Notes = Notes
            };
        }
    }
}