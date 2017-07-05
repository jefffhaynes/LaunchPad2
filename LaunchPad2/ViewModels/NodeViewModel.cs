using System;
using System.Collections.ObjectModel;
using System.Linq;
using NodeControl;
using XBee;

namespace LaunchPad2.ViewModels
{
    public class NodeViewModel : ViewModelBase
    {
        private LongAddress _address;
        private bool _discovering;
        private bool _isArmed;
        private bool _isEnabled;
        private string _name;
        private NodeDiscoveryState _nodeDiscoveryState;
        private string _notes;
        private SignalStrength? _signalStrength;

        public NodeViewModel(string name, LongAddress address,
            SignalStrength? signalStrength = XBee.SignalStrength.Low,
            NodeDiscoveryState discoveryState = NodeDiscoveryState.None)
        {
            Name = name;
            Address = address;
            IsEnabled = true;
            SignalStrength = signalStrength;
            DiscoveryState = discoveryState;

            Ports = new ReadOnlyCollection<PortViewModel>(new[]
            {
                new PortViewModel(NodeControl.Ports.Port0),
                new PortViewModel(NodeControl.Ports.Port1),
                new PortViewModel(NodeControl.Ports.Port2),
                new PortViewModel(NodeControl.Ports.Port3),
                new PortViewModel(NodeControl.Ports.Port4),
                new PortViewModel(NodeControl.Ports.Port5),
                new PortViewModel(NodeControl.Ports.Port6),
                new PortViewModel(NodeControl.Ports.Port7)
            });

            foreach (var port in Ports)
                port.StateChanged += PortOnStateChanged;
        }

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

        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public LongAddress Address
        {
            get { return _address; }
            set
            {
                if (!Equals(_address, value))
                {
                    _address = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool Discovering
        {
            get { return _discovering; }
            set
            {
                if (_discovering != value)
                {
                    _discovering = value;
                    OnPropertyChanged();
                }
            }
        }

        public NodeDiscoveryState DiscoveryState
        {
            get { return _nodeDiscoveryState; }
            set
            {
                if (_nodeDiscoveryState != value)
                {
                    _nodeDiscoveryState = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsArmed
        {
            get { return _isArmed; }
            set
            {
                if (_isArmed != value)
                {
                    _isArmed = value;
                    OnPropertyChanged();
                }
            }
        }

        public SignalStrength? SignalStrength
        {
            get { return _signalStrength; }
            set
            {
                if (_signalStrength != value)
                {
                    _signalStrength = value;
                    OnPropertyChanged();
                }
            }
        }

        public ReadOnlyCollection<PortViewModel> Ports { get; }

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

        public bool IsDirty
        {
            get { return Ports.Any(port => port.IsDirty); }
        }

        private void PortOnStateChanged(object sender, EventArgs eventArgs)
        {
            SyncPortStates();
        }

        public void SyncPortStates()
        {
            if (!IsDirty)
                return;

            var portsToActivate = Ports.Where(port => port.ShouldBeActive).Select(port => port.Port).ToArray();
            var portState = !portsToActivate.Any()
                ? NodeControl.Ports.None
                : portsToActivate.Aggregate((ports, port) => ports | port);

            foreach (var port in Ports)
                port.IsKnownActive = port.ShouldBeActive;

            try
            {
                NetworkController.SetActivePorts(new NodeAddress(Address), portState);
            }
            catch (TimeoutException)
            {
            }
        }
    }
}