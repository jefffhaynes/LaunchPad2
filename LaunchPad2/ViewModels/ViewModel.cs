using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using FMOD;
using LaunchPad2.Models;
using NodeControl;
using XBee;

namespace LaunchPad2.ViewModels
{
    public class ViewModel : ViewModelBase
    {
        private const double DefaultZoom = 0.1;
        private const string ClipboardTracksKey = "Tracks";
        private static readonly TimeSpan DefaultCuePosition = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan DefaultCueLength = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan CountdownLength = TimeSpan.FromSeconds(10);
        private string _audioFile;
        private AudioTrack _audioTrack;
        private Dictionary<EventCueViewModel, TrackViewModel> _clonedCuesAndTracks;
        private CancellationTokenSource _countdownCancellationTokenSource;
        private TimeSpan _countdownTime;
        private List<CueMoveInfo> _cueUndoStates;
        private string _file;
        private bool _isShowRunning;
        private NetworkDiscoveryState _networkDiscoveryState;
        private object _selectedItem;
        private double _zoom;

        public event EventHandler Stopped;

        public ViewModel()
        {
            UndoCommand = new RelayCommand(UndoManager.Undo);
            RedoCommand = new RelayCommand(UndoManager.Redo);

            PlayCommand = new RelayCommand(() => AudioTrack.IsPaused = !AudioTrack.IsPaused, IsAudioFileLoaded);
            StopCommand = new RelayCommand(async () => { await Stop(); });

            StartShowCommand = new RelayCommand(async () =>
            {
                _countdownCancellationTokenSource = new CancellationTokenSource();

                AudioTrack.Position = TimeSpan.Zero;
                CountdownTime = CountdownLength;

                await Arm();
                IsShowRunning = true;

                DateTime start = DateTime.Now;
                while (CountdownTime > TimeSpan.Zero && !_countdownCancellationTokenSource.IsCancellationRequested)
                {
                    CountdownTime = CountdownLength - (DateTime.Now - start);
                    await Task.Delay(25);
                }

                if (!_countdownCancellationTokenSource.IsCancellationRequested)
                    AudioTrack.IsPaused = false;

            }, IsAudioFileLoaded);

            AbortShowCommand = new RelayCommand(async () =>
            {
                _countdownCancellationTokenSource.Cancel();
                await Stop();
            });

            PositionCommand = new RelayCommand(position =>
            {
                if (AudioTrack != null)
                    AudioTrack.SamplePosition = (uint) position;
            });

            AddTrackCommand = new RelayCommand(() => AddTrack(), IsAudioFileLoaded);
            AddCueCommand = new RelayCommand(AddCue);
            DeleteCommand = new RelayCommand(Delete);
            CueAlignLeftCommand = new RelayCommand(() => DoUndoableCueMove(CueAlignLeft));
            CueAlignRightCommand = new RelayCommand(() => DoUndoableCueMove(CueAlignRight));
            CueAlignAllCommand = new RelayCommand(() => DoUndoableCueMove(CueAlignAll));
            CueMakeSameWidthCommand = new RelayCommand(() => DoUndoableCueMove(CueMakeSameWidth));
            CueDistributeLeftCommand = new RelayCommand(() => DoUndoableCueMove(CueDistributeLeft));
            CueDistributeLeftReverseCommand = new RelayCommand(() => DoUndoableCueMove(CueDistributeLeftReverse));

            CueDistributeOnBeatsCommand = new RelayCommand(CueDistributeOnBeats);

            AddDeviceCommand = new RelayCommand(AddDevice);
            DeleteDeviceCommand = new RelayCommand(DeleteDevice);
            AddTrackFromDeviceCommand = new RelayCommand(device => AddTrack((DeviceViewModel) device));

            GroupCommand = new RelayCommand(GroupSelected);
            UngroupCommand = new RelayCommand(UngroupSelected);

            ZoomExtentsCommand = new RelayCommand(width => ZoomExtents((double) width));

            DiscoverNetworkCommand = new RelayCommand(DiscoverNetwork);

            Tracks = new ObservableCollection<TrackViewModel>();
            Devices = new ObservableCollection<DeviceViewModel>();
            Nodes = new ObservableCollection<NodeViewModel>();
            Groups = new ObservableCollection<EventCueGroupViewModel>();

            NetworkController.NodeDiscovered += NetworkControllerOnNodeDiscovered;
            NetworkController.InitializingController += NetworkControllerOnInitializingController;
            NetworkController.DiscoveringNetwork += NetworkControllerOnDiscoveringNetwork;
            Nodes.CollectionChanged += NodesOnCollectionChanged;

            Zoom = DefaultZoom;

            CompositionTarget.Rendering += CompositionTargetOnRendering;
        }

        private string _status;

        public string Status
        {
            get { return _status; }

            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }

        public async void SetStatus(string status)
        {
            Status = status;
            await Task.Delay(TimeSpan.FromSeconds(10));
            Status = null;
        }

        public string File
        {
            get { return _file; }
            set
            {
                if (_file != value)
                {
                    _file = value;
                    OnPropertyChanged();
                }
            }
        }

        public TimeSpan CountdownTime
        {
            get { return _countdownTime; }
            set
            {
                if (_countdownTime != value)
                {
                    _countdownTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsShowRunning
        {
            get { return _isShowRunning; }
            set
            {
                if (_isShowRunning != value)
                {
                    _isShowRunning = value;
                    OnPropertyChanged();
                }
            }
        }

        public string AudioFile
        {
            get { return _audioFile; }
            set
            {
                if (_audioFile != value)
                {
                    _audioFile = value;
                    AudioTrack = new AudioTrack(value);

                    OnPropertyChanged();
                }
            }
        }

        public AudioTrack AudioTrack
        {
            get { return _audioTrack; }
            set
            {
                if (_audioTrack != value)
                {
                    if (_audioTrack != null)
                    {
                        _audioTrack.PositionChanged -= AudioTrackOnPositionChanged;
                    }

                    _audioTrack = value;
                    PlayCommand.UpdateCanExecute();
                    StartShowCommand.UpdateCanExecute();
                    AddTrackCommand.UpdateCanExecute();
                    UpdateSampleRate();

                    if (_audioTrack != null)
                    {
                        _audioTrack.PositionChanged += AudioTrackOnPositionChanged;
                    }
                    OnPropertyChanged();
                }
            }
        }


        public ObservableCollection<TrackViewModel> Tracks { get; set; }

        public ObservableCollection<DeviceViewModel> Devices { get; set; }

        public ObservableCollection<NodeViewModel> Nodes { get; set; }

        public ObservableCollection<EventCueGroupViewModel> Groups { get; set; }

        public IEnumerable<EventCueViewModel> AllCues
        {
            get { return Tracks.SelectMany(track => track.Cues); }
        }

        public RelayCommand UndoCommand { get; private set; }

        public RelayCommand RedoCommand { get; private set; }

        public RelayCommand PlayCommand { get; private set; }

        public RelayCommand StopCommand { get; private set; }

        public RelayCommand StartShowCommand { get; private set; }

        public RelayCommand AbortShowCommand { get; private set; }

        public RelayCommand PositionCommand { get; private set; }

        public RelayCommand AddTrackCommand { get; private set; }

        public ICommand AddCueCommand { get; private set; }

        public ICommand DeleteCommand { get; private set; }

        public ICommand CueAlignLeftCommand { get; private set; }

        public ICommand CueAlignRightCommand { get; private set; }

        public ICommand CueAlignAllCommand { get; private set; }

        public ICommand CueMakeSameWidthCommand { get; private set; }

        public ICommand CueDistributeLeftCommand { get; private set; }

        public ICommand CueDistributeLeftReverseCommand { get; private set; }

        public ICommand CueDistributeOnBeatsCommand { get; private set; }

        public ICommand AddDeviceCommand { get; private set; }

        public ICommand DeleteDeviceCommand { get; private set; }

        public ICommand AddTrackFromDeviceCommand { get; private set; }

        public ICommand DiscoverNetworkCommand { get; private set; }

        public ICommand GroupCommand { get; private set; }

        public ICommand UngroupCommand { get; private set; }

        public ICommand ZoomExtentsCommand { get; private set; }

        public IList<object> SelectedItems { get; set; }

        public double Zoom
        {
            get { return _zoom; }
            set
            {
                if (Math.Abs(_zoom - value) > double.Epsilon)
                {
                    _zoom = value;
                    OnPropertyChanged();
                }
            }
        }

        public object SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    OnPropertyChanged();
                }
            }
        }

        public NetworkDiscoveryState NetworkDiscoveryState
        {
            get { return _networkDiscoveryState; }
            set
            {
                if (_networkDiscoveryState != value)
                {
                    _networkDiscoveryState = value;
                    OnPropertyChanged();
                }
            }
        }

        private void CompositionTargetOnRendering(object sender, EventArgs eventArgs)
        {
            if (AudioTrack != null && !AudioTrack.IsPaused)
                AudioTrack.Update();
        }

        private async Task Stop()
        {
            if (IsShowRunning)
            {
                await Disarm();
                IsShowRunning = false;
            }

            if (AudioTrack != null)
            {
                AudioTrack.IsPaused = true;
                AudioTrack.Position = TimeSpan.Zero;
            }

            var handler = Stopped;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        private async Task Arm()
        {
            foreach (NodeViewModel node in Nodes)
                await NetworkController.Arm(new NodeAddress(node.Address));
        }

        private async Task Disarm()
        {
            foreach (NodeViewModel node in Nodes)
                await NetworkController.Disarm(new NodeAddress(node.Address));
        }

        private IEnumerable<TrackViewModel> GetSelectedTracks()
        {
            return SelectedItems == null
                ? Enumerable.Empty<TrackViewModel>()
                : SelectedItems.OfType<TrackViewModel>();
        }

        private IEnumerable<EventCueViewModel> GetSelectedCues()
        {
            IEnumerable<EventCueViewModel> cues = SelectedItems == null
                ? Enumerable.Empty<EventCueViewModel>()
                : SelectedItems.OfType<EventCueViewModel>();

            return cues.Union(GetSelectedTracks().SelectMany(track => track.Cues));
        }

        private void AudioTrackOnPositionChanged(object sender, EventArgs eventArgs)
        {
            TimeSpan position = _audioTrack.Position;

            // Make a copy so we don't step on anything else going on
            List<TrackViewModel> tracks = Tracks.ToList();

            foreach (TrackViewModel track in tracks)
            {
                bool trackActive = false;

                // Make a copy so we don't step on anything else going on
                List<EventCueViewModel> cues = track.Cues.ToList();

                foreach (EventCueViewModel cue in cues)
                {
                    if (cue.Intersects(position))
                    {
                        cue.IsActive = true;
                        trackActive = true;
                    }
                    else cue.IsActive = false;
                }

                if (track.Port != null)
                    track.Port.ShouldBeActive = trackActive;
            }

            foreach (NodeViewModel node in Nodes)
            {
                node.SyncPortStates();
            }
        }

        private bool IsAudioFileLoaded()
        {
            return AudioTrack != null;
        }

        private void AddTrack(DeviceViewModel device = null)
        {
            string trackName = string.Format("Track {0}", Tracks.Count);
            var track = new TrackViewModel {Name = trackName, Device = device};
            var doAction = new Action(() => Tracks.Add(track));
            var undoAction = new Action(() => Tracks.Remove(track));

            UndoManager.DoAndAdd(doAction, undoAction);
        }

        private void AddCue()
        {
            if (AudioTrack == null)
                return;

            TimeSpan position = AudioTrack.Position;

            /* Put it someplace useful */
            if (position == TimeSpan.Zero && AudioTrack.Length > DefaultCuePosition)
                position = DefaultCuePosition;

            float sampleRate = AudioTrack.SampleRate;
            Dictionary<TrackViewModel, EventCueViewModel> trackCues = GetSelectedTracks().ToDictionary(track => track,
                track => new EventCueViewModel(sampleRate, position, DefaultCueLength));

            var doAction = new Action(() =>
            {
                foreach (var pair in trackCues)
                    pair.Key.AddCue(pair.Value);
            });

            var undoAction = new Action(() =>
            {
                foreach (var pair in trackCues)
                    pair.Key.RemoveCue(pair.Value);
            });

            UndoManager.DoAndAdd(doAction, undoAction);
        }

        private void Delete()
        {
            var undoBatchMemento = new UndoBatchMemento();

            IEnumerable<TrackViewModel> tracksToRemove = GetSelectedTracks().ToList();

            /* Keep a copy so we can simulate removal and get a correct index for undo */
            List<TrackViewModel> shadowTracks = Tracks.ToList();

            foreach (TrackViewModel trackToRemove in tracksToRemove)
            {
                TrackViewModel track = trackToRemove;
                int index = shadowTracks.IndexOf(track);
                shadowTracks.RemoveAt(index);

                var doAction = new Action(() => Tracks.RemoveAt(index));
                var undoAction = new Action(() => Tracks.Insert(index, track));
                undoBatchMemento.Add(doAction, undoAction);

                SelectedItems.Remove(trackToRemove);
            }

            Delete(GetSelectedCues(), undoBatchMemento);

            UndoManager.DoAndAdd(undoBatchMemento);
        }

        private void Delete(IEnumerable<EventCueViewModel> cues, UndoBatchMemento undoBatchMemento)
        {
            List<EventCueViewModel> cuesToRemove = cues.ToList();

            foreach (TrackViewModel track in Tracks)
            {
                ObservableCollection<EventCueViewModel> trackCues = track.Cues;
                List<EventCueViewModel> matchingCues = trackCues.Intersect(cuesToRemove).ToList();

                foreach (EventCueViewModel matchingCue in matchingCues)
                {
                    EventCueViewModel cue = matchingCue;

                    var doAction = new Action(() => trackCues.Remove(cue));
                    var undoAction = new Action(() => trackCues.Add(cue));
                    undoBatchMemento.Add(doAction, undoAction);

                    cuesToRemove.Remove(matchingCue);
                }

                if (cuesToRemove.Count == 0)
                    break;
            }
        }

        private void UpdateSampleRate()
        {
            if (AudioTrack == null)
                return;

            foreach (EventCueViewModel cue in AllCues)
                cue.SampleRate = AudioTrack.SampleRate;
        }

        public void StartMove()
        {
            SetCueMoveUndo();

            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) && SelectedItems != null)
            {
                /* Clone selected cues and leave a copy in place */
                IEnumerable<EventCueViewModel> selectedCues = SelectedItems.OfType<EventCueViewModel>();
                _clonedCuesAndTracks = selectedCues.ToDictionary(cue => cue.Clone(),
                    cue => Tracks.Single(track => track.Cues.Contains(cue)));

                foreach (var clonedCueAndTrack in _clonedCuesAndTracks)
                    clonedCueAndTrack.Value.Cues.Add(clonedCueAndTrack.Key);
            }
            else _clonedCuesAndTracks = null;
        }

        public void EndMove()
        {
            if (_clonedCuesAndTracks == null)
            {
                CommitCueMoveUndo();
            }
            else
            {
                var clonedCuesAndTracks = new Dictionary<EventCueViewModel, TrackViewModel>(_clonedCuesAndTracks);
                var doAction = new Action(() =>
                {
                    foreach (var clonedCueAndTrack in clonedCuesAndTracks)
                        clonedCueAndTrack.Value.Cues.Add(clonedCueAndTrack.Key);
                });

                var undoAction = new Action(() =>
                {
                    foreach (var clonedCueAndTrack in clonedCuesAndTracks)
                        clonedCueAndTrack.Value.Cues.Remove(clonedCueAndTrack.Key);
                });

                var batchUndoMemento = new UndoBatchMemento();
                batchUndoMemento.Add(doAction, undoAction);

                CommitCueMoveUndo(batchUndoMemento);
            }
        }

        private void SetCueMoveUndo()
        {
            var selectedCues = AllCues.Where(cue => cue.IsSelected).ToList();

            if (!selectedCues.Any())
            {
                _cueUndoStates = null;
                return;
            }

            _cueUndoStates = selectedCues.Select(cue => new CueMoveInfo(cue, cue.Clone())).ToList();
        }

        private void CommitCueMoveUndo(UndoBatchMemento undoBatchMemento = null)
        {
            if (_cueUndoStates == null)
                return;

            List<CueMoveInfo> cueStates = AllCues.Where(cue => cue.IsSelected)
                .Select(cue => new CueMoveInfo(cue, cue.Clone())).ToList();

            List<CueMoveInfo> undoCueStates = _cueUndoStates.ToList();

            /* Check for no movement */
            var firstUndoState = undoCueStates.First();
            if (!firstUndoState.HasChanged)
                return;

            var doAction = new Action(() =>
            {
                foreach (CueMoveInfo cueInfo in cueStates)
                {
                    cueInfo.Cue.Start = cueInfo.Before.Start;
                    cueInfo.Cue.Length = cueInfo.Before.Length;
                    cueInfo.Cue.LeadIn = cueInfo.Before.LeadIn;
                }
            });

            var undoAction = new Action(() =>
            {
                foreach (CueMoveInfo cueUndoInfo in undoCueStates)
                {
                    cueUndoInfo.Cue.Start = cueUndoInfo.Before.Start;
                    cueUndoInfo.Cue.Length = cueUndoInfo.Before.Length;
                    cueUndoInfo.Cue.LeadIn = cueUndoInfo.Before.LeadIn;
                }
            });

            if (undoBatchMemento == null)
                UndoManager.Add(doAction, undoAction);
            else
            {
                undoBatchMemento.Add(doAction, undoAction);
                UndoManager.Add(undoBatchMemento);
            }
        }

        private void DoUndoableCueMove(Action cueAction)
        {
            SetCueMoveUndo();
            cueAction();
            CommitCueMoveUndo();
        }

        private void CueAlignLeft()
        {
            if (!GetSelectedCues().Any())
                return;

            double start = GetSelectedCues().Min(cue => cue.StartSample);

            foreach (EventCueViewModel cue in GetSelectedCues())
                cue.StartSample = (uint) start;
        }

        private void CueAlignRight()
        {
            if (!GetSelectedCues().Any())
                return;

            double end = GetSelectedCues().Max(cue => cue.EndSample);

            foreach (EventCueViewModel cue in GetSelectedCues())
                cue.EndSample = (uint) end;
        }

        private void CueAlignAll()
        {
            if (!GetSelectedCues().Any())
                return;

            CueAlignLeft();
            CueMakeSameWidth();
        }

        private void CueMakeSameWidth()
        {
            if (!GetSelectedCues().Any())
                return;

            double length = GetSelectedCues().Max(cue => cue.SampleLength);

            foreach (EventCueViewModel cue in GetSelectedCues())
                cue.SampleLength = (uint) length;
        }

        private void CueDistributeLeft()
        {
            CueDistributeLeft(GetSelectedCues());
        }

        private void CueDistributeLeftReverse()
        {
            CueDistributeLeft(GetSelectedCues().Reverse());
        }

        private void CueDistributeLeft(IEnumerable<EventCueViewModel> cues)
        {
            List<EventCueViewModel> selectedCues = GetSelectedCues().ToList();

            if (selectedCues.Count < 2)
                return;

            double min = selectedCues.Min(cue => cue.StartSample);
            double max = selectedCues.Max(cue => cue.StartSample);
            double range = max - min;
            double spacing = range/(selectedCues.Count() - 1);

            int i = 0;
            foreach (EventCueViewModel cue in cues)
            {
                cue.StartSample = (uint) (min + spacing*i);
                i++;
            }
        }

        private void CueDistributeOnBeats(object parameter)
        {
            int offset = Convert.ToInt32(parameter);
            CueDistributeOnBands(offset, 1);
        }

        private void CueDistributeOnBands(int bandOffset, int bandCount)
        {
            var undoBatchMemento = new UndoBatchMemento();

            IEnumerable<double[]> subbands = AudioTrack.EnergySubbands.Skip(bandOffset).Take(bandCount);
            List<double> subbandAverages = subbands.ZipMany(band => band.Average()).ToList();

            double average;
            double stdDev = subbandAverages.StdDev(out average);

            double millisecondsPerValue = AudioTrack.Length.TotalMilliseconds/subbandAverages.Count;
            IEnumerable<SampleInfo<double>> energyAndTime =
                subbandAverages.Select((value, i) =>
                    new SampleInfo<double>(TimeSpan.FromMilliseconds(i*millisecondsPerValue), value));

            List<SampleInfo<double>> energyAndTimeOrderedByEnergy =
                energyAndTime.Where(sample => sample.Value > average + stdDev)
                    .OrderByDescending(value => value.Value)
                    .ToList();

            List<TrackViewModel> selectedTracks = GetSelectedTracks().ToList();

            if (selectedTracks.Count == 0)
            {
                SetCueMoveUndo();

                List<EventCueViewModel> selectedCues = GetSelectedCues().ToList();

                int valueIndex = 0;
                foreach (EventCueViewModel cue in selectedCues)
                {
                    List<SampleInfo<double>> nearestNeighborOrdered =
                        energyAndTimeOrderedByEnergy.OrderBy(
                            sample => Math.Abs((cue.Start - sample.Time).TotalMilliseconds)).ToList();

                    do
                    {
                        SampleInfo<double> sample = nearestNeighborOrdered[valueIndex++];
                        cue.Start = sample.Time;
                        //cue.LeadIn = TimeSpan.FromMilliseconds(sample.Value);
                    } while (
                        selectedCues.Where(c => c != cue).Any(c => c.Intersects(cue)) &&
                        valueIndex < energyAndTimeOrderedByEnergy.Count);
                }

                CommitCueMoveUndo(undoBatchMemento);
            }
            else
            {
                /* Wipe out and populate selected tracks */
                foreach (TrackViewModel track in selectedTracks)
                {
                    Delete(track.Cues, undoBatchMemento);

                    /* Higher energy values have shorter duration */
                    List<EventCueViewModel> cues =
                        energyAndTimeOrderedByEnergy.Select(
                            value =>
                            {
                                const int durationMs = 500;
                                return new EventCueViewModel(AudioTrack.SampleRate, value.Time,
                                    TimeSpan.FromMilliseconds(durationMs));
                            })
                            .ToList();

                    ObservableCollection<EventCueViewModel> trackCues = track.Cues;
                    var doAction = new Action(() =>
                    {
                        foreach (EventCueViewModel cue in cues)
                        {
                            if (!trackCues.Any(c => c.Intersects(cue)))
                                trackCues.Add(cue);
                        }
                    });

                    undoBatchMemento.Add(doAction, trackCues.Clear);
                }
            }

            UndoManager.DoAndAdd(undoBatchMemento);
        }

        private void AddDevice()
        {
            string deviceName = string.Format("Device {0}", Devices.Count);
            var device = new DeviceViewModel {Name = deviceName};

            var doAction = new Action(() => Devices.Add(device));
            var undoAction = new Action(() => Devices.Remove(device));

            UndoManager.DoAndAdd(doAction, undoAction);
        }

        private void DeleteDevice()
        {
            var device = SelectedItem as DeviceViewModel;

            if (device == null)
                return;

            var undoBatchMemento = new UndoBatchMemento();

            int index = Devices.IndexOf(device);
            var doAction = new Action(() => Devices.RemoveAt(index));
            var undoAction = new Action(() => Devices.Insert(index, device));

            undoBatchMemento.Add(doAction, undoAction);

            List<TrackViewModel> affectedTracks = Tracks.Where(track => track.Device == device).ToList();

            foreach (TrackViewModel affectedTrack in affectedTracks)
            {
                TrackViewModel track = affectedTrack;
                undoBatchMemento.Add(() => track.Device = null, () => track.Device = device);
            }

            UndoManager.DoAndAdd(undoBatchMemento);
        }

        private void ZoomExtents(double width)
        {
            if (_audioTrack == null)
            {
                Zoom = DefaultZoom;
                return;
            }

            Zoom = width/_audioTrack.TotalSamples*100;
        }

        public void Cut()
        {
            Copy();
            Delete();
        }

        public void Copy()
        {
            if (GetSelectedTracks().Any())
            {
                List<TrackModel> tracks = GetSelectedTracks().Select(track => new TrackModel(track)).ToList();
                Clipboard.SetData(ClipboardTracksKey, tracks);
            }
        }

        public void Paste()
        {
            if (Clipboard.ContainsData(ClipboardTracksKey))
            {
                var tracks = (List<TrackModel>) Clipboard.GetData(ClipboardTracksKey);
                List<TrackViewModel> trackViewModels =
                    tracks.Select(track => track.GetViewModel(Devices, Nodes)).ToList();

                var doAction = new Action(() =>
                {
                    foreach (TrackViewModel track in trackViewModels)
                    {
                        string trackName = GetUniqueName(track.Name);
                        track.Name = trackName;

                        TrackViewModel insertTrack = GetSelectedTracks().LastOrDefault();

                        if (insertTrack == null)
                            Tracks.Add(track);
                        else
                        {
                            int insertIndex = Tracks.IndexOf(insertTrack);
                            Tracks.Insert(insertIndex + 1, track);
                        }
                    }

                    UpdateSampleRate();
                });

                var undoAction = new Action(() =>
                {
                    foreach (TrackViewModel track in trackViewModels)
                        Tracks.Remove(track);
                });

                UndoManager.DoAndAdd(doAction, undoAction);
            }
        }

        private string GetUniqueName(string baseName)
        {
            string name = baseName;
            for (int i = 0; Tracks.Select(track => track.Name).Contains(name); i++)
            {
                name = string.Format("{0} - Copy", baseName);

                if (i != 0)
                    name += string.Format(" {0}", i);
            }

            return name;
        }

        private void GroupSelected()
        {
            IEnumerable<IGroupable> selected = SelectedItems.OfType<IGroupable>();
            List<IGroupable> rootGroupables = selected.Select(item => item.GetRootGroupable()).Distinct().ToList();

            var group = new EventCueGroupViewModel();
            group.Children = new ObservableCollection<IGroupable>(rootGroupables);

            UndoManager.DoAndAdd(() =>
            {
                foreach (IGroupable rootGroupable in rootGroupables)
                    rootGroupable.Group = group;
                Groups.Add(group);
            }, () =>
            {
                foreach (IGroupable rootGroupable in rootGroupables)
                    rootGroupable.Group = null;
                Groups.Remove(group);
            });
        }

        private void UngroupSelected()
        {
            IEnumerable<IGroupable> selected = SelectedItems.OfType<IGroupable>();
            List<IGroupable> rootGroupables = selected.Select(item => item.GetRootGroupable()).Distinct().ToList();

            IEnumerable<EventCueGroupViewModel> groups = rootGroupables.OfType<EventCueGroupViewModel>().ToList();

            UndoManager.DoAndAdd(() =>
            {
                foreach (EventCueGroupViewModel group in groups)
                {
                    foreach (IGroupable child in group.Children)
                        child.Group = null;
                    Groups.Remove(group);
                }
            }, () =>
            {
                foreach (EventCueGroupViewModel group in groups)
                {
                    foreach (IGroupable child in group.Children)
                        child.Group = group;
                    Groups.Add(group);
                }
            });
        }

        private async void DiscoverNetwork()
        {
            if (Nodes == null)
            {
                MessageBox.Show("Nodes is null!!");
                return;
            }

            foreach (NodeViewModel node in Nodes)
                node.DiscoveryState = NodeDiscoveryState.Discovering;

            try
            {
                await NetworkController.DiscoverNetworkAsync();
                NetworkDiscoveryState = NetworkDiscoveryState.Discovered;
            }
            catch (TimeoutException)
            {
                NetworkDiscoveryState = NetworkDiscoveryState.Failed;
            }
            catch (InvalidOperationException)
            {
                NetworkDiscoveryState = NetworkDiscoveryState.Failed;
                MessageBox.Show("No XBee controller found.");
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }


        private void NetworkControllerOnDiscoveringNetwork(object sender, EventArgs eventArgs)
        {
            NetworkDiscoveryState = NetworkDiscoveryState.Discovering;
        }

        private void NetworkControllerOnInitializingController(object sender, EventArgs eventArgs)
        {
            NetworkDiscoveryState = NetworkDiscoveryState.Initializing;
        }

        private void NetworkControllerOnNodeDiscovered(object sender, NodeDiscoveredEventArgs e)
        {
            XBeeNode node = e.Node;

            NodeViewModel existingNode = Nodes.FirstOrDefault(n => n.Address.Equals(node.Address.LongAddress));

            //var name = await node.GetNodeIdentifier();
            string name = e.Name;

            SignalStrength? signalStrength = e.SignalStrength.HasValue ? e.SignalStrength : SignalStrength.High;

            if (existingNode == null)
            {
                var nodeViewModel = new NodeViewModel(name, node.Address.LongAddress, signalStrength,
                    NodeDiscoveryState.Discovered);

                Nodes.Add(nodeViewModel);
            }
            else
            {
                existingNode.Name = name;
                existingNode.SignalStrength = signalStrength;
                existingNode.DiscoveryState = NodeDiscoveryState.Discovered;
            }
        }

        private void NodesOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (NodeViewModel item in e.NewItems.Cast<NodeViewModel>())
                    item.PropertyChanged += OnNodeViewModelOnPropertyChanged;
            }

            if (e.OldItems != null)
            {
                foreach (NodeViewModel item in e.OldItems.Cast<NodeViewModel>())
                    item.PropertyChanged -= OnNodeViewModelOnPropertyChanged;
            }
        }


        private async void OnNodeViewModelOnPropertyChanged(object o, PropertyChangedEventArgs args)
        {
            var n = (NodeViewModel) o;
            if (args.PropertyName == "Name")
            {
                try
                {
                    await NetworkController.SetNodeName(new NodeAddress(n.Address), n.Name);
                }
                catch (TimeoutException)
                {
                }
            }
        }
    }
}