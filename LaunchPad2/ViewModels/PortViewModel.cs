using System;
using NodeControl;

namespace LaunchPad2.ViewModels
{
    public class PortViewModel : ViewModelBase
    {
        private bool _isActive;
        private bool _shouldBeActive;

        public PortViewModel(Ports port)
        {
            Port = port;
        }

        public string Name => Port.GetEnumDescription();

        public Ports Port { get; }

        /// <summary>
        /// Used to indicate that this port should be active, but hasn't necessarily been synced with the remote device yet.
        /// </summary>
        public bool ShouldBeActive
        {
            get { return _shouldBeActive; }
            set
            {
                if (_shouldBeActive != value)
                {
                    _shouldBeActive = value;
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
                    ShouldBeActive = value;

                    StateChanged?.Invoke(this, EventArgs.Empty);

                    OnPropertyChanged();
                    OnPropertyChanged("IsKnownActive");
                }
            }
        }

        public bool IsKnownActive
        {
            get { return _isActive; }

            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    OnPropertyChanged();
                    OnPropertyChanged("IsActive");
                }
            }
        }

        public bool IsDirty => ShouldBeActive != IsActive;

        public event EventHandler StateChanged;
    }
}