using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LaunchPad2.Controls;
using LaunchPad2.Models;
using LaunchPad2.ViewModels;
using Microsoft.Win32;
using NodeControl;

namespace LaunchPad2
{
    public partial class MainWindow : Window
    {
// ReSharper disable once NotAccessedField.Local
        private TemporaryFile _temporaryAudioFile;
        private ViewModel _viewModel = new ViewModel();
        private bool _audioFileChanged;
        private readonly SemaphoreSlim _saveLock = new SemaphoreSlim(1);

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _viewModel;
            _viewModel.Stopped += (sender, args) => AudioScrollViewer.ScrollToHorizontalOffset(0);
            Closing += (sender, args) => SerialPortService.CleanUp();
        }

        private void LoadAudioButtonOnClick(object sender, RoutedEventArgs e)
        {
            _viewModel.IsWorking = true;

            try
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "Audio Files|*.mp3;*.wav",
                    Multiselect = false
                };

                bool? result = dialog.ShowDialog();
                if (result != null && result.Value)
                {
                    _viewModel.AudioFile = dialog.FileName;
                }

                _audioFileChanged = true;
            }
            finally
            {
                _viewModel.IsWorking = false;
            }
        }

        private async void SaveButtonOnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel.IsWorking = true;

                await _saveLock.WaitAsync();

                if (_viewModel.File == null || !File.Exists(_viewModel.File))
                    SaveAsButtonOnClick(sender, e);
                else
                {
                    _viewModel.SetStatus("Saving...");
                    var model = new Model(_viewModel);

                    await Task.Run(() =>
                    {
                        if (_audioFileChanged)
                            Packager.Pack(_viewModel.File, model, _viewModel.AudioFile);
                        else Packager.Update(_viewModel.File, model);
                    });

                    _viewModel.SetStatus("Saved");
                }
            }
            finally
            {
                _saveLock.Release();

                _viewModel.IsWorking = false;
            }
        }

        private async void SaveAsButtonOnClick(object sender, RoutedEventArgs e)
        {
            _viewModel.IsWorking = true;

            try
            {
                var dialog = new SaveFileDialog
                {
                    FileName = _viewModel.File,
                    Filter = "LaunchPad Files|*.lpx"
                };

                bool? result = dialog.ShowDialog();
                if (result != null && result.Value)
                {
                    _viewModel.SetStatus("Saving...");
                    var model = new Model(_viewModel);
                    await Task.Run(() => Packager.Pack(dialog.FileName, model, _viewModel.AudioFile));

                    _viewModel.File = dialog.FileName;
                    _viewModel.SetStatus("Saved");
                }
            }
            finally
            {
                _viewModel.IsWorking = false;
            }
        }

        private async void LoadButtonOnClick(object sender, RoutedEventArgs e)
        {
            _viewModel.IsWorking = true;

            try
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "LaunchPad Files|*.lpx",
                    Multiselect = false
                };

                bool? result = dialog.ShowDialog();
                if (result != null && result.Value)
                {
                    _viewModel.SetStatus("Loading...");

                    string filename = dialog.FileName;

                    Model model = null;
                    TemporaryFile temporaryAudioFile = null;

                    await Task.Run(() => model = Packager.Unpack(filename, out temporaryAudioFile));

                    _viewModel = model.GetViewModel();
                    DataContext = _viewModel;

                    if (temporaryAudioFile != null)
                        _viewModel.AudioFile = temporaryAudioFile.Path;

                    _temporaryAudioFile = temporaryAudioFile;
                    _viewModel.File = dialog.FileName;

                    _viewModel.Stopped += (s, args) => AudioScrollViewer.ScrollToHorizontalOffset(0);

                    _viewModel.SetStatus("Loaded");
                }
            }
            finally
            {
                _viewModel.IsWorking = false;
            }
        }

        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            ScrollViewer scrollViewer = Equals(sender, AudioScrollViewer) ? TrackScrollViewer : AudioScrollViewer;

            scrollViewer.ScrollToHorizontalOffset(e.HorizontalOffset);
            TimelineScrollViewer.ScrollToHorizontalOffset(e.HorizontalOffset);

            /* Try to keep scroll position on zoom */
            if (Math.Abs(e.ExtentWidthChange) > double.Epsilon)
            {
                double originalExtent = e.ExtentWidth - e.ExtentWidthChange;

                if (Math.Abs(originalExtent) > double.Epsilon)
                {
                    double changeRatio = e.ExtentWidth/originalExtent;
                    scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset*changeRatio);
                    TimelineScrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset * changeRatio);
                }
            }
        }

        private void CommandBinding_OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Undo)
                UndoManager.Undo();
            else if (e.Command == ApplicationCommands.Redo)
                UndoManager.Redo();
            else if (e.Command == ApplicationCommands.Cut)
                _viewModel.Cut();
            else if (e.Command == ApplicationCommands.Copy)
                _viewModel.Copy();
            else if (e.Command == ApplicationCommands.Paste)
                _viewModel.Paste();
        }

        private void SelectionCanvas_OnCueMoving(object sender, CueMovingEventArgs e)
        {
            _viewModel.StartMove();
        }

        private void SelectionCanvas_OnCueMoved(object sender, EventArgs e)
        {
            _viewModel.EndMove();
        }
    }
}