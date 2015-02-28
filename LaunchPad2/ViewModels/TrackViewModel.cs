using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;

namespace LaunchPad2.ViewModels
{
    public class TrackViewModel : ViewModelBase
    {
        private static readonly Brush DefaultBrush = new SolidColorBrush(Color.FromArgb(255, 0, 97, 163));

        public static readonly ReadOnlyCollection<Brush> _brushes = new ReadOnlyCollection<Brush>(
            new List<Brush>(new[]
            {
                DefaultBrush,
                new SolidColorBrush(Color.FromRgb(201, 81, 0)),
                new SolidColorBrush(Color.FromRgb(175, 100, 170)),
                new SolidColorBrush(Color.FromRgb(254, 215, 0)),
                new SolidColorBrush(Color.FromRgb(226, 21, 0)),
                new SolidColorBrush(Color.FromRgb(121, 113, 8))
            }));

        private Brush _brush;
        private DeviceViewModel _device;
        private string _name;
        private NodeViewModel _node;
        private string _notes;
        private PortViewModel _port;

        public TrackViewModel() : this(null, DefaultBrush)
        {
            Cues = new ObservableCollection<EventCueViewModel>();
        }

        public TrackViewModel(string name, Brush brush)
        {
            _name = name;
            _brush = brush;

            ClearDeviceCommand = new RelayCommand(ClearDevice);
            ClearNodeCommand = new RelayCommand(ClearNode);
        }

        public static IEnumerable<BrushViewModel> Brushes
        {
            get { return _brushes.Select(brush => new BrushViewModel(brush)); }
        }

        public ObservableCollection<EventCueViewModel> Cues { get; set; }

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

        public Brush Brush
        {
            get { return _brush; }
            set
            {
                if (_brush.ToString() != value.ToString())
                {
                    var doAction = new Action(() =>
                    {
                        _brush = value;
                        OnPropertyChanged();
                    });

                    Brush originalColor = _brush != null ? _brush.Clone() : null;
                    var undoAction = new Action(() =>
                    {
                        _brush = originalColor;
                        OnPropertyChanged();
                    });

                    UndoManager.DoAndAdd(doAction, undoAction);
                }
            }
        }

        public DeviceViewModel Device
        {
            get { return _device; }
            set
            {
                if (_device != value)
                {
                    if (_device != null)
                        _device.PropertyChanged -= DevicePropertyChanged;

                    _device = value;

                    if (_device != null)
                        _device.PropertyChanged += DevicePropertyChanged;

                    UpdateCues();
                    OnPropertyChanged();
                }
            }
        }

        public NodeViewModel Node
        {
            get { return _node; }
            set
            {
                if (_node != value)
                {
                    _node = value;
                    OnPropertyChanged();
                }
            }
        }

        public PortViewModel Port
        {
            get { return _port; }
            set
            {
                if (_port != value)
                {
                    _port = value;
                    OnPropertyChanged();
                }
            }
        }

        //private bool _isActive;

        //public bool IsActive
        //{
        //    get { return _isActive; }
        //    set
        //    {
        //        if (_isActive != value)
        //        {
        //            _isActive = value;

        //            if(Port != null)
        //                Port.IsActive = value;
        //            OnPropertyChanged();
        //        }
        //    }
        //}

        public ICommand ClearDeviceCommand { get; private set; }

        public ICommand ClearNodeCommand { get; private set; }

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

        public void AddCue(EventCueViewModel cue)
        {
            Cues.Add(cue);

            if (Device != null)
            {
                cue.LeadIn = Device.LeadIn;
                cue.Length = Device.Length;
                cue.IsLockedToDevice = true;
            }
        }

        public void RemoveCue(EventCueViewModel cue)
        {
            Cues.Remove(cue);
        }

        private void ClearDevice()
        {
            if (Device == null)
                return;

            var doAction = new Action(() => Device = null);

            DeviceViewModel device = _device;
            var undoAction = new Action(() => Device = device);

            doAction();

            UndoManager.Add(doAction, undoAction);
        }

        private void ClearNode()
        {
            if (Node == null)
                return;

            var doAction = new Action(() => Node = null);

            NodeViewModel node = _node;
            var undoAction = new Action(() => Node = node);

            doAction();

            UndoManager.Add(doAction, undoAction);
        }

        private void DevicePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateCues();
        }

        private void UpdateCues()
        {
            foreach (EventCueViewModel cue in Cues)
            {
                if (Device != null)
                {
                    cue.Length = Device.Length;
                    cue.LeadIn = Device.LeadIn;
                    cue.IsLockedToDevice = true;
                }
                else cue.IsLockedToDevice = false;
            }
        }

        public TrackViewModel Clone()
        {
            return new TrackViewModel
            {
                Cues = new ObservableCollection<EventCueViewModel>(Cues),
                Device = Device,
                Node = Node,
                Port = Port,
                Brush = Brush,
                Name = string.Format("{0} - Copy", Name)
            };
        }
    }
}