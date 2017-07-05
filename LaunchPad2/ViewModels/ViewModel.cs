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
    public class ViewModel : ViewModelBase, IDisposable
    {
        private const double DefaultZoom = 0.1;
        private const string ClipboardTracksKey = "Tracks";
        private static readonly TimeSpan DefaultCuePosition = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan DefaultCueLength = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan CountdownLength = TimeSpan.FromSeconds(10);
        private readonly Dictionary<PortViewModel, bool> _portStates = new Dictionary<PortViewModel, bool>();
        private string _audioFile;
        private AudioTrack _audioTrack;
        private Dictionary<EventCueViewModel, TrackViewModel> _clonedCuesAndTracks;
        private CancellationTokenSource _countdownCancellationTokenSource;
        private TimeSpan _countdownTime;
        private List<CueMoveInfo> _cueUndoStates;
        private string _file;
        private bool _isNetworkArming;
        private bool _isShowRunning;
        private bool _isWorking;
        private NetworkDiscoveryState _networkDiscoveryState;
        private bool _repeat;
        private object _selectedItem;
        private TimeSpan _selectedRegionLength;
        private TimeSpan _selectedRegionStart;
        private string _status;
        private double _zoom;
        private bool _isConfirming;
        private bool _isShowStarting;

        public ViewModel()
        {
            UndoCommand = new RelayCommand(UndoManager.Undo);
            RedoCommand = new RelayCommand(UndoManager.Redo);

            PlayCommand = new RelayCommand(Play, IsAudioFileLoaded);
            StopCommand = new RelayCommand(async () => { await Stop(); });

            StartShowCommand = new RelayCommand(StartShow, IsAudioFileLoaded);

            PositionCommand = new RelayCommand(position =>
            {
                if (AudioTrack != null)
                    AudioTrack.SamplePosition = (uint) position;
            });

            AddTrackCommand = new RelayCommand(() => AddTrack(), IsAudioFileLoaded);
            AddCueCommand = new RelayCommand(AddCue);
            DeleteCueCommand = new RelayCommand(DeleteCue);
            DeleteCommand = new RelayCommand(Delete);
            CalculateBeatsCommand = new RelayCommand(CalculateBeats, IsAudioFileLoaded);
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

            ZoomExtentsCommand = new RelayCommand(width => ZoomExtents((double) width - 32));
            // 32 is for track header (yeah, total kludge)

            DiscoverNetworkCommand = new RelayCommand(async () => await DiscoverNetwork());
            NetworkDiscoveryResetCommand = new RelayCommand(async () => await ResetNetworkDiscovery());
            DeleteNodeCommand = new RelayCommand(DeleteNode);
            ArmNetworkCommand = new RelayCommand(async () => await Arm());
            DisarmNetworkCommand = new RelayCommand(async () => await Disarm());

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

        public bool IsWorking
        {
            get { return _isWorking; }
            set
            {
                if (_isWorking != value)
                {
                    _isWorking = value;
                    OnPropertyChanged();
                }
            }
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

                IsWorking = value;
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

        public bool Repeat
        {
            get { return _repeat; }
            set
            {
                if (_repeat != value)
                {
                    _repeat = value;

                    if (AudioTrack != null)
                        AudioTrack.Repeat = value;

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
                        _audioTrack.StatusChanged -= AudioTrackOnStatusChanged;
                    }

                    _audioTrack = value;
                    PlayCommand.UpdateCanExecute();
                    StartShowCommand.UpdateCanExecute();
                    AddTrackCommand.UpdateCanExecute();
                    CalculateBeatsCommand.UpdateCanExecute();

                    UpdateSampleRate();

                    if (_audioTrack != null)
                    {
                        _audioTrack.Repeat = Repeat;
                        _audioTrack.PositionChanged += AudioTrackOnPositionChanged;
                        _audioTrack.StatusChanged += AudioTrackOnStatusChanged;
                    }

                    OnPropertyChanged();
// ReSharper disable ExplicitCallerInfoArgument
                    OnPropertyChanged("SampleRate");
// ReSharper restore ExplicitCallerInfoArgument
                }
            }
        }

        public float SampleRate
        {
            get
            {
                if (AudioTrack == null)
                    return 0;

                return AudioTrack.SampleRate;
            }
        }

        public ObservableCollection<TrackViewModel> Tracks { get; set; }
        public ObservableCollection<DeviceViewModel> Devices { get; set; }
        public ObservableCollection<NodeViewModel> Nodes { get; set; }
        public IEnumerable<NodeViewModel> EnabledNodes => Nodes.Where(node => node.IsEnabled);
        public ObservableCollection<EventCueGroupViewModel> Groups { get; set; }

        public IEnumerable<EventCueViewModel> AllCues
        {
            get { return Tracks.SelectMany(track => track.Cues); }
        }

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
                    _selectedItem = null;
                    OnPropertyChanged();
                    _selectedItem = value;
                    OnPropertyChanged();
                }
            }
        }

        public TimeSpan SelectedRegionStart
        {
            get { return _selectedRegionStart; }
            set
            {
                if (_selectedRegionStart != value)
                {
                    _selectedRegionStart = value;
                    OnPropertyChanged();
// ReSharper disable ExplicitCallerInfoArgument
                    OnPropertyChanged("SelectedRegionStartSample");
// ReSharper restore ExplicitCallerInfoArgument
                }
            }
        }

        public TimeSpan SelectedRegionLength
        {
            get { return _selectedRegionLength; }
            set
            {
                if (_selectedRegionLength != value)
                {
                    _selectedRegionLength = value;
                    OnPropertyChanged();
// ReSharper disable ExplicitCallerInfoArgument
                    OnPropertyChanged("SelectedRegionSampleLength");
// ReSharper restore ExplicitCallerInfoArgument
                }
            }
        }

        public TimeSpan SelectedRegionEnd => SelectedRegionStart + SelectedRegionLength;

        public uint SelectedRegionStartSample
        {
            get { return AudioTrack == null ? 0 : (uint) AudioTrack.ToSamples(SelectedRegionStart); }
            set
            {
                if (AudioTrack == null)
                    return;

                SelectedRegionStart = AudioTrack.ToTime(value);
            }
        }

        public int SelectedRegionSampleLength
        {
            get { return AudioTrack == null ? 0 : (int) AudioTrack.ToSamples(SelectedRegionLength); }
            set
            {
                if (AudioTrack == null)
                    return;

                SelectedRegionLength = AudioTrack.ToTime(value);
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

        public bool IsNetworkArming
        {
            get { return _isNetworkArming; }
            set
            {
                if (_isNetworkArming != value)
                {
                    _isNetworkArming = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsNetworkDisarmed => EnabledNodes.All(node => !node.IsArmed);

        public void Dispose()
        {
            AudioTrack?.Dispose();
        }

        public event EventHandler Stopped;

        public async void SetStatus(string status)
        {
            Status = status;
            await Task.Delay(TimeSpan.FromSeconds(3));
            Status = "Ready";
        }

        private async void Play()
        {
            if (AudioTrack.SamplePosition == 0)
                await Stop();

            if (!IsNetworkDisarmed)
            {
                IsConfirming = true;
            }
            else
            {
                AudioTrack.IsPaused = !AudioTrack.IsPaused;
            }
        }

        private async Task Stop()
        {
            IsConfirming = false;
            IsShowStarting = false;

            _countdownCancellationTokenSource?.Cancel();

            if (AudioTrack != null)
            {
                AudioTrack.IsPaused = true;
                AudioTrack.Position = TimeSpan.Zero;
            }

            if (IsShowRunning)
            {
                await Disarm();
                IsShowRunning = false;
                SetStatus("Show Aborted");
            }
            
            Stopped?.Invoke(this, EventArgs.Empty);
        }

        public bool IsConfirming
        {
            get { return _isConfirming; }
            set
            {
                if (_isConfirming == value)
                    return;

                _isConfirming = value;
                OnPropertyChanged();
            }
        }

        public bool IsShowStarting
        {
            get { return _isShowStarting; }
            set
            {
                if (_isShowStarting == value)
                    return;

                _isShowStarting = value;
                OnPropertyChanged();
            }
        }

        private async void StartShow()
        {
            IsConfirming = false;

            try
            {
                await NetworkController.Initialize();
            }
            catch(InvalidOperationException)
            {
                MessageBox.Show("No network controller found.");
            }
            
            SetStatus("Starting Show");

            IsShowStarting = true;
            IsShowRunning = true;

            _countdownCancellationTokenSource = new CancellationTokenSource();

            AudioTrack.Position = TimeSpan.Zero;


            CountdownTime = CountdownLength;
            
            var start = DateTime.Now;
            while (CountdownTime > TimeSpan.Zero && !_countdownCancellationTokenSource.IsCancellationRequested)
            {
                CountdownTime = CountdownLength - (DateTime.Now - start);
                await Task.Delay(25);
            }

            if (!_countdownCancellationTokenSource.IsCancellationRequested)
                AudioTrack.IsPaused = false;

            IsShowStarting = false;

            SetStatus("Show Running");
        }

        public async Task Arm()
        {
            IsNetworkArming = true;

            try
            {
                var nodes = EnabledNodes.Where(node => node.DiscoveryState == NodeDiscoveryState.Discovered);
                await Task.WhenAll(nodes.Select(Arm));
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message);
            }
            finally
            {
                IsNetworkArming = false;
            }
        }

        private static async Task Arm(NodeViewModel node)
        {
            try
            {
                await NetworkController.Arm(new NodeAddress(node.Address));
                node.IsArmed = true;
            }
            catch(InvalidOperationException)
            {
            }
            catch (TimeoutException)
            {
            }
        }

        public async Task Disarm()
        {
            IsNetworkArming = true;
            
            try
            {
                var nodes = EnabledNodes.Where(node => node.DiscoveryState == NodeDiscoveryState.Discovered);
                await Task.WhenAll(nodes.Select(Disarm));
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
            finally
            {
                IsNetworkArming = false;
            }
        }

        private static async Task Disarm(NodeViewModel node)
        {
            try
            {
                await NetworkController.Disarm(new NodeAddress(node.Address));
                node.IsArmed = false;
            }
            catch (InvalidOperationException)
            {
            }
            catch (TimeoutException)
            {
            }
        }

        private IEnumerable<TrackViewModel> GetSelectedTracks()
        {
            return SelectedItems?.OfType<TrackViewModel>() ?? Enumerable.Empty<TrackViewModel>();
        }

        private IEnumerable<EventCueViewModel> GetSelectedCues()
        {
            var cues = SelectedItems?.OfType<EventCueViewModel>() ?? Enumerable.Empty<EventCueViewModel>();

            return cues.Union(GetSelectedTracks().SelectMany(track => track.Cues));
        }

        private void AudioTrackOnPositionChanged(object sender, EventArgs eventArgs)
        {
            // This is the business end of the whole thing.

            var position = _audioTrack.Position;

            // Make a copy so we don't step on anything else going on
            var tracks = Tracks.ToList();

            _portStates.Clear();

            foreach (var track in tracks)
            {
                var trackActive = false;

                // Make a copy so we don't step on anything else going on
                var cues = track.Cues.ToList();

                foreach (var cue in cues)
                {
                    if (cue.Intersects(position))
                    {
                        cue.IsActive = true;
                        trackActive = true;
                    }
                    else cue.IsActive = false;
                }

                if (track.Port != null)
                {
                    bool portState;
                    if (_portStates.TryGetValue(track.Port, out portState))
                        _portStates[track.Port] = portState | trackActive;
                    else _portStates.Add(track.Port, trackActive);
                }
            }

            foreach (var port in _portStates)
            {
                port.Key.ShouldBeActive = port.Value;
            }

            foreach (var node in EnabledNodes)
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
            var trackName = $"Track {Tracks.Count}";
            var track = new TrackViewModel {Name = trackName, Device = device};
            var doAction = new Action(() => Tracks.Add(track));
            var undoAction = new Action(() => Tracks.Remove(track));

            UndoManager.DoAndAdd(doAction, undoAction);
        }

        private void AddCue()
        {
            if (AudioTrack == null)
                return;

            var position = AudioTrack.Position;

            /* Put it someplace useful */
            if (position == TimeSpan.Zero && AudioTrack.Length > DefaultCuePosition)
                position = DefaultCuePosition;

            var sampleRate = SampleRate;
            var trackCues = GetSelectedTracks().ToDictionary(track => track,
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

        private void DeleteCue()
        {
            var undoBatchMemento = new UndoBatchMemento();
            DeleteCue(undoBatchMemento);
            UndoManager.DoAndAdd(undoBatchMemento);
        }

        private void DeleteCue(UndoBatchMemento undoBatchMemento)
        {
            var cues = SelectedRegionLength > TimeSpan.Zero
                ? GetSelectedCues().Where(cue => cue.Start > SelectedRegionStart && cue.Start < SelectedRegionEnd)
                : GetSelectedCues();

            BatchDelete(cues, undoBatchMemento);
        }

        private void Delete()
        {
            var undoBatchMemento = new UndoBatchMemento();

            IEnumerable<TrackViewModel> tracksToRemove = GetSelectedTracks().ToList();

            /* Keep a copy so we can simulate removal and get a correct index for undo */
            var shadowTracks = Tracks.ToList();

            foreach (var trackToRemove in tracksToRemove)
            {
                var track = trackToRemove;
                var index = shadowTracks.IndexOf(track);
                shadowTracks.RemoveAt(index);

                var doAction = new Action(() => Tracks.RemoveAt(index));
                var undoAction = new Action(() => Tracks.Insert(index, track));
                undoBatchMemento.Add(doAction, undoAction);

                SelectedItems.Remove(trackToRemove);
            }

            BatchDelete(GetSelectedCues(), undoBatchMemento);

            UndoManager.DoAndAdd(undoBatchMemento);
        }

        private void BatchDelete(IEnumerable<EventCueViewModel> cues, UndoBatchMemento undoBatchMemento)
        {
            var cuesToRemove = cues.ToList();

            foreach (var track in Tracks)
            {
                var trackCues = track.Cues;
                var matchingCues = trackCues.Intersect(cuesToRemove).ToList();

                foreach (var matchingCue in matchingCues)
                {
                    var cue = matchingCue;

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

            foreach (var cue in AllCues)
                cue.SampleRate = SampleRate;
        }

        public void StartMove()
        {
            SetCueMoveUndo();

            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) && SelectedItems != null)
            {
                /* Clone selected cues and leave a copy in place */
                var selectedCues = SelectedItems.OfType<EventCueViewModel>();
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

            var cueStates = AllCues.Where(cue => cue.IsSelected)
                .Select(cue => new CueMoveInfo(cue, cue.Clone())).ToList();

            var undoCueStates = _cueUndoStates.ToList();

            /* Check for no movement */
            var firstUndoState = undoCueStates.First();
            if (!firstUndoState.HasChanged)
                return;

            /* Needed only for redo */
            var doAction = new Action(() =>
            {
                foreach (var cueInfo in cueStates)
                {
                    cueInfo.Cue.Start = cueInfo.Before.Start;
                    cueInfo.Cue.Length = cueInfo.Before.Length;
                    cueInfo.Cue.LeadIn = cueInfo.Before.LeadIn;
                }
            });

            var undoAction = new Action(() =>
            {
                foreach (var cueUndoInfo in undoCueStates)
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

        private void CalculateBeats()
        {
            AudioTrack?.CalculateEnergySubbands();
        }

        private void CueAlignLeft()
        {
            if (!GetSelectedCues().Any())
                return;

            double start = GetSelectedCues().Min(cue => cue.StartSample);

            foreach (var cue in GetSelectedCues())
                cue.StartSample = (uint) start;
        }

        private void CueAlignRight()
        {
            if (!GetSelectedCues().Any())
                return;

            double end = GetSelectedCues().Max(cue => cue.EndSample);

            foreach (var cue in GetSelectedCues())
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

            foreach (var cue in GetSelectedCues())
                cue.SampleLength = (int) length;
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
            var selectedCues = GetSelectedCues().ToList();

            if (selectedCues.Count < 2)
                return;

            double min = selectedCues.Min(cue => cue.StartSample);
            double max = selectedCues.Max(cue => cue.StartSample);
            var range = max - min;
            var spacing = range/(selectedCues.Count - 1);

            var i = 0;
            foreach (var cue in cues)
            {
                cue.StartSample = (uint) (min + spacing*i);
                i++;
            }
        }

        private void CueDistributeOnBeats(object parameter)
        {
            var offset = Convert.ToInt32(parameter);
            CueDistributeOnBands(offset, 1);
        }

        private void CueDistributeOnBands(int bandOffset, int bandCount)
        {
            var undoBatchMemento = new UndoBatchMemento();

            var subbands = AudioTrack.EnergySubbands.Skip(bandOffset).Take(bandCount);
            var subbandAverages = subbands.ZipMany(band => band.Average()).ToList();

            double average;
            var stdDev = subbandAverages.StdDev(out average);

            var millisecondsPerValue = AudioTrack.Length.TotalMilliseconds/subbandAverages.Count;
            var energyAndTime =
                subbandAverages.Select((value, i) =>
                    new SampleInfo<double>(TimeSpan.FromMilliseconds(i*millisecondsPerValue), value));

            var energyAndTimeOrderedByEnergy =
                energyAndTime.Where(sample => sample.Value > average + stdDev)
                    .OrderByDescending(value => value.Value)
                    .ToList();

            var selectedTracks = GetSelectedTracks().ToList();

            if (selectedTracks.Count == 0)
            {
                SetCueMoveUndo();

                var selectedCues = GetSelectedCues().ToList();

                var valueIndex = 0;
                foreach (var cue in selectedCues)
                {
                    var nearestNeighborOrdered =
                        energyAndTimeOrderedByEnergy.OrderBy(
                            sample => Math.Abs((cue.Start - sample.Time).TotalMilliseconds)).ToList();

                    do
                    {
                        var sample = nearestNeighborOrdered[valueIndex++];
                        cue.Start = sample.Time;
                    } while (
                        selectedCues.Where(c => c != cue).Any(c => c.Intersects(cue)) &&
                        valueIndex < energyAndTimeOrderedByEnergy.Count);
                }

                CommitCueMoveUndo(undoBatchMemento);
            }
            else
            {
                /* Wipe out and populate selected tracks */
                foreach (var track in selectedTracks)
                {
                    DeleteCue(undoBatchMemento);

                    var sampleDuration = TimeSpan.FromMilliseconds(500);

                    var regionalEnergyAndTimeOrderedByEnergy = SelectedRegionLength > TimeSpan.Zero
                        ? energyAndTimeOrderedByEnergy.Where(
                            sample =>
                                sample.Time > SelectedRegionStart && sample.Time + sampleDuration < SelectedRegionEnd)
                        : energyAndTimeOrderedByEnergy;

                    var cues =
                        regionalEnergyAndTimeOrderedByEnergy.Select(
                            value => new EventCueViewModel(SampleRate, value.Time, sampleDuration))
                            .ToList();

                    var trackCues = track.Cues;
                    var doAction = new Action(() =>
                    {
                        foreach (var cue in cues)
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
            var deviceName = $"Device {Devices.Count}";
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

            var index = Devices.IndexOf(device);
            var doAction = new Action(() => Devices.RemoveAt(index));
            var undoAction = new Action(() => Devices.Insert(index, device));

            undoBatchMemento.Add(doAction, undoAction);

            var affectedTracks = Tracks.Where(track => track.Device == device).ToList();

            foreach (var affectedTrack in affectedTracks)
            {
                var track = affectedTrack;
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
                var tracks = GetSelectedTracks().Select(track => new TrackModel(track)).ToList();
                Clipboard.SetData(ClipboardTracksKey, tracks);
            }
        }

        public void Paste()
        {
            if (Clipboard.ContainsData(ClipboardTracksKey))
            {
                var tracks = (List<TrackModel>) Clipboard.GetData(ClipboardTracksKey);
                var trackViewModels =
                    tracks.Select(track => track.GetViewModel(Devices, Nodes)).ToList();

                var doAction = new Action(() =>
                {
                    foreach (var track in trackViewModels)
                    {
                        var trackName = GetUniqueName(track.Name);
                        track.Name = trackName;

                        var insertTrack = GetSelectedTracks().LastOrDefault();

                        if (insertTrack == null)
                            Tracks.Add(track);
                        else
                        {
                            var insertIndex = Tracks.IndexOf(insertTrack);
                            Tracks.Insert(insertIndex + 1, track);
                        }
                    }

                    UpdateSampleRate();
                });

                var undoAction = new Action(() =>
                {
                    foreach (var track in trackViewModels)
                        Tracks.Remove(track);
                });

                UndoManager.DoAndAdd(doAction, undoAction);
            }
        }

        private string GetUniqueName(string baseName)
        {
            var name = baseName;
            for (var i = 0; Tracks.Select(track => track.Name).Contains(name); i++)
            {
                name = $"{baseName} - Copy";

                if (i != 0)
                    name += $" {i}";
            }

            return name;
        }

        private void GroupSelected()
        {
            var selected = SelectedItems.OfType<IGroupable>();
            var rootGroupables = selected.Select(item => item.GetRootGroupable()).Distinct().ToList();

            var group = new EventCueGroupViewModel();
            group.Children = new ObservableCollection<IGroupable>(rootGroupables);

            UndoManager.DoAndAdd(() =>
            {
                foreach (var rootGroupable in rootGroupables)
                    rootGroupable.Group = group;
                Groups.Add(group);
                group.Select();
            }, () =>
            {
                group.Unselect();
                foreach (var rootGroupable in rootGroupables)
                    rootGroupable.Group = null;
                Groups.Remove(group);
            });
        }

        private void UngroupSelected()
        {
            var selected = SelectedItems.OfType<IGroupable>();
            var rootGroupables = selected.Select(item => item.GetRootGroupable()).Distinct().ToList();

            IEnumerable<EventCueGroupViewModel> groups = rootGroupables.OfType<EventCueGroupViewModel>().ToList();

            UndoManager.DoAndAdd(() =>
            {
                foreach (var group in groups)
                {
                    group.Unselect();
                    foreach (var child in group.Children)
                        child.Group = null;
                    Groups.Remove(group);
                }
            }, () =>
            {
                foreach (var group in groups)
                {
                    foreach (var child in group.Children)
                        child.Group = group;
                    Groups.Add(group);
                    group.Select();
                }
            });
        }

        public async Task DiscoverNetwork()
        {
            foreach (var node in Nodes)
                node.Discovering = true;

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
                NetworkDiscoveryState = NetworkDiscoveryState.Failed;
                MessageBox.Show(e.Message);
            }
            finally
            {
                foreach (var node in Nodes)
                    node.Discovering = false;
            }
        }

        public async Task ResetNetworkDiscovery()
        {
            if (MessageBox.Show("Reset network discovery?", "LaunchPad", MessageBoxButton.OKCancel) ==
                MessageBoxResult.OK)
            {
                await Disarm();
                foreach (var node in Nodes)
                    node.DiscoveryState = NodeDiscoveryState.None;
            }
        }

        private void DeleteNode()
        {
            var node = SelectedItem as NodeViewModel;

            if (node == null)
                return;

            var undoBatchMemento = new UndoBatchMemento();

            var index = Nodes.IndexOf(node);
            var doAction = new Action(() => Nodes.RemoveAt(index));
            var undoAction = new Action(() => Nodes.Insert(index, node));

            undoBatchMemento.Add(doAction, undoAction);

            var affectedTracks = Tracks.Where(track => track.Node == node).ToList();

            foreach (var affectedTrack in affectedTracks)
            {
                var track = affectedTrack;
                undoBatchMemento.Add(() => track.Node = null, () => track.Node = node);
            }

            UndoManager.DoAndAdd(undoBatchMemento);
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
            var node = e.Node;

            var existingNode = Nodes.FirstOrDefault(n => n.Address.Equals(node.Address.LongAddress));

            var name = e.Name;
            var signalStrength = e.SignalStrength.HasValue ? e.SignalStrength : SignalStrength.High;

            if (existingNode == null)
            {
                var nodeViewModel = new NodeViewModel(name, node.Address.LongAddress, signalStrength,
                    NodeDiscoveryState.Discovered);

                Nodes.Add(nodeViewModel);
            }
            else
            {
                existingNode.SetNameInteral(name);
                existingNode.SignalStrength = signalStrength;
                existingNode.DiscoveryState = NodeDiscoveryState.Discovered;
            }
        }

        private void NodesOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems.Cast<NodeViewModel>())
                    item.NameChanged += OnNodeViewModelNameChanged;
            }

            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems.Cast<NodeViewModel>())
                    item.NameChanged -= OnNodeViewModelNameChanged;
            }
        }

        private async void OnNodeViewModelNameChanged(object o, EventArgs args)
        {
            var n = (NodeViewModel)o;

            try
            {
                await NetworkController.SetNodeName(new NodeAddress(n.Address), n.Name);
            }
            catch (TimeoutException)
            {
            }
        }

        private void AudioTrackOnStatusChanged(object sender, AudioTrackStatusEventArgs e)
        {
            SetStatus(e.Message);
        }

        private void CompositionTargetOnRendering(object sender, EventArgs eventArgs)
        {
            if (AudioTrack != null && !AudioTrack.IsPaused)
                AudioTrack.Update();
        }

        #region Commands

        public RelayCommand UndoCommand { get; private set; }

        public RelayCommand RedoCommand { get; private set; }

        public RelayCommand PlayCommand { get; }

        public RelayCommand StopCommand { get; private set; }

        public RelayCommand StartShowCommand { get; }

        public RelayCommand PositionCommand { get; private set; }

        public RelayCommand AddTrackCommand { get; }

        public ICommand AddCueCommand { get; private set; }

        public ICommand DeleteCueCommand { get; private set; }

        public ICommand DeleteCommand { get; private set; }

        public RelayCommand CalculateBeatsCommand { get; }

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

        public ICommand NetworkDiscoveryResetCommand { get; private set; }

        public ICommand DeleteNodeCommand { get; private set; }

        public ICommand ArmNetworkCommand { get; private set; }

        public ICommand DisarmNetworkCommand { get; private set; }

        public ICommand GroupCommand { get; private set; }

        public ICommand UngroupCommand { get; private set; }

        public ICommand ZoomExtentsCommand { get; private set; }

        #endregion
    }
}