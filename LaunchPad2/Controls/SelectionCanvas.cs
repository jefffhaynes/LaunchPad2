using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace LaunchPad2.Controls
{
    public class SelectionCanvas : Canvas
    {
        public static readonly RoutedEvent CueSelectedEvent = EventManager.RegisterRoutedEvent(
            "CueSelected", RoutingStrategy.Bubble, typeof (RoutedEventHandler), typeof (SelectionCanvas));

        public static readonly RoutedEvent CueMovingEvent = EventManager.RegisterRoutedEvent(
            "CueMoving", RoutingStrategy.Bubble, typeof (EventHandler<CueMovingEventArgs>), typeof (SelectionCanvas));

        public static readonly RoutedEvent CueMoveEvent = EventManager.RegisterRoutedEvent(
            "CueMove", RoutingStrategy.Bubble, typeof (EventHandler<CueMoveEventArgs>), typeof (SelectionCanvas));

        public static readonly RoutedEvent CueMovedEvent = EventManager.RegisterRoutedEvent(
            "CueMoved", RoutingStrategy.Bubble, typeof (EventHandler), typeof (SelectionCanvas));

        public static readonly RoutedEvent TrackSelectedEvent = EventManager.RegisterRoutedEvent(
            "TrackSelected", RoutingStrategy.Bubble, typeof (RoutedEventHandler), typeof (SelectionCanvas));

        public static readonly DependencyProperty SelectionBrushProperty = DependencyProperty.Register(
            "SelectionBrush", typeof (Brush), typeof (SelectionCanvas),
            new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0, 121, 203)), SelectionBrushChangedCallback));

        public static readonly DependencyProperty AutoscrollDistanceProperty = DependencyProperty.Register(
            "AutoscrollDistance", typeof (double), typeof (SelectionCanvas), new PropertyMetadata(5.0));

        public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.Register(
            "SelectedItems", typeof (IList), typeof (SelectionCanvas),
            new FrameworkPropertyMetadata(default(IList),
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
            "SelectedItem", typeof (object), typeof (SelectionCanvas),
            new FrameworkPropertyMetadata(default(object), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty SelectedCueProperty = DependencyProperty.Register(
            "SelectedCue", typeof (EventCueControl), typeof (SelectionCanvas),
            new PropertyMetadata(default(EventCueControl)));

        public static readonly DependencyProperty SelectedCuesProperty = DependencyProperty.Register(
            "SelectedCues", typeof (IList), typeof (SelectionCanvas),
            new FrameworkPropertyMetadata(default(IList),
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty SelectedTracksProperty = DependencyProperty.Register(
            "SelectedTracks", typeof (IList), typeof (SelectionCanvas),
            new FrameworkPropertyMetadata(default(IList),
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty DeleteCommandProperty = DependencyProperty.Register(
            "DeleteCommand", typeof (ICommand), typeof (SelectionCanvas), new PropertyMetadata(default(ICommand)));

        public static readonly DependencyProperty AddCueCommandProperty = DependencyProperty.Register(
            "AddCueCommand", typeof (ICommand), typeof (SelectionCanvas), new PropertyMetadata(default(ICommand)));

        private readonly Rectangle _selectionRectangle;
        private Point _clickPosition;
        private ReadOnlyDictionary<EventCueControl, Rect> _cueControlBounds;
        private CueMoveMode _cueMoveMode;
        private ReadOnlyDictionary<EventCueControl, CueInfo> _cuePositionAndLength;
        private bool _moving;
        private ScrollViewer _scrollViewer;
        private bool _selecting;

        public SelectionCanvas()
        {
            _selectionRectangle = new Rectangle
            {
                Stroke = SelectionBrush,
                StrokeDashArray = new DoubleCollection(new[] {5.0, 5.0}),
                Visibility = Visibility.Hidden
            };

            Children.Add(_selectionRectangle);

            CueSelected += SelectionCanvasCueSelected;
            CueMoving += SelectionCanvasCueMoving;
            CueMove += SelectionCanvasCueMove;
            CueMoved += SelectionCanvas_CueMoved;
            TrackSelected += SelectionCanvasTrackSelected;
            MouseLeftButtonDown += SelectionCanvasMouseLeftButtonDown;
            MouseMove += SelectionCanvasMouseMove;
            MouseLeftButtonUp += SelectionCanvasMouseLeftButtonUp;
            KeyDown += SelectionCanvasKeyDown;
        }

        public object SelectedItem
        {
            get { return GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public IList SelectedTracks
        {
            get { return (IList) GetValue(SelectedTracksProperty); }
            set { SetValue(SelectedTracksProperty, value); }
        }

        public IList SelectedCues
        {
            get { return (IList) GetValue(SelectedCuesProperty); }
            set { SetValue(SelectedCuesProperty, value); }
        }

        public ICommand AddCueCommand
        {
            get { return (ICommand) GetValue(AddCueCommandProperty); }
            set { SetValue(AddCueCommandProperty, value); }
        }

        public EventCueControl SelectedCue
        {
            get { return (EventCueControl) GetValue(SelectedCueProperty); }
            set { SetValue(SelectedCueProperty, value); }
        }

        public IList SelectedItems
        {
            get { return (IList) GetValue(SelectedItemsProperty); }
            set { SetValue(SelectedItemsProperty, value); }
        }

        public ICommand DeleteCommand
        {
            get { return (ICommand) GetValue(DeleteCommandProperty); }
            set { SetValue(DeleteCommandProperty, value); }
        }

        public Brush SelectionBrush
        {
            get { return (Brush) GetValue(SelectionBrushProperty); }
            set { SetValue(SelectionBrushProperty, value); }
        }

        public double AutoscrollDistance
        {
            get { return (double) GetValue(AutoscrollDistanceProperty); }
            set { SetValue(AutoscrollDistanceProperty, value); }
        }

        private IEnumerable<EventCueControl> Cues
        {
            get { return FindVisualChildren<EventCueControl>(); }
        }

        private IEnumerable<TrackHeaderControl> Tracks
        {
            get { return FindVisualChildren<TrackHeaderControl>(); }
        }

        private IEnumerable<EventCueControl> GetSelectedCues()
        {
            return Cues.Where(cue => cue.IsSelected);
        }

        private IEnumerable<TrackHeaderControl> GetSelectedTracks()
        {
            return Tracks.Where(track => track.IsSelected);
        }

        private void SelectionCanvasCueMoving(object sender, CueMovingEventArgs e)
        {
            Dictionary<EventCueControl, CueInfo> cuePositionAndLength = GetSelectedCues().ToDictionary(
                control => control,
                control => new CueInfo(control.Sample, control.SampleLength, control.LeadInSampleLength));

            _cuePositionAndLength = new ReadOnlyDictionary<EventCueControl, CueInfo>(cuePositionAndLength);

            if (_cuePositionAndLength.Count == 0)
                return;

            _cueMoveMode = e.CueMoveMode;

            _moving = true;
        }

        private void SelectionCanvasCueMove(object sender, CueMoveEventArgs e)
        {
            if (_moving)
            {
                Dictionary<EventCueControl, double> updatedCuePositions = null;
                Dictionary<EventCueControl, double> updatedCueLengths = null;
                Dictionary<EventCueControl, double> updatedCueLeadInLengths = null;

                double lengthOverrunAdjustment = 0;

                if (_cueMoveMode == CueMoveMode.LeftGrip || _cueMoveMode == CueMoveMode.RightGrip)
                {
                    double delta = _cueMoveMode == CueMoveMode.RightGrip ? e.Delta : -e.Delta;

                    /* Update lengths */
                    updatedCueLengths = _cuePositionAndLength.ToDictionary(pair => pair.Key,
                        pair => pair.Value.Length + delta);

                    /* Make sure all lengths are positive */
                    lengthOverrunAdjustment = updatedCueLengths.Min(pair =>
                        pair.Value - pair.Key.GetNaturalScaledSize().Width);

                    if (lengthOverrunAdjustment < 0)
                    {
                        updatedCueLengths = updatedCueLengths.ToDictionary(pair => pair.Key,
                            pair => pair.Value - lengthOverrunAdjustment);
                    }
                }

                if (_cueMoveMode == CueMoveMode.LeftGrip || _cueMoveMode == CueMoveMode.Normal)
                {
                    /* Update position */
                    updatedCuePositions = _cuePositionAndLength.ToDictionary(pair => pair.Key,
                        pair => pair.Value.Position + e.Delta);

                    if (lengthOverrunAdjustment < 0)
                    {
                        updatedCuePositions = updatedCuePositions.ToDictionary(pair => pair.Key,
                            pair => pair.Value + lengthOverrunAdjustment);
                    }

                    /* Make sure all positions are positive */
                    double overrunAdjustment = updatedCuePositions.Min(pair => pair.Value);

                    if (overrunAdjustment < 0)
                    {
                        updatedCuePositions = updatedCuePositions.ToDictionary(pair => pair.Key,
                            pair => pair.Value - overrunAdjustment);
                    }
                }

                if (_cueMoveMode == CueMoveMode.LeadIn)
                {
                    updatedCueLeadInLengths = _cuePositionAndLength.ToDictionary(pair => pair.Key,
                        pair => pair.Value.LeadInLength - e.Delta);

                    double overrunAdjustment = updatedCueLeadInLengths.Min(pair => pair.Value);

                    if (overrunAdjustment < 0)
                    {
                        updatedCueLeadInLengths = updatedCueLeadInLengths.ToDictionary(pair => pair.Key,
                            pair => pair.Value - overrunAdjustment);
                    }
                }

                switch (_cueMoveMode)
                {
                    case CueMoveMode.RightGrip:
                        if (updatedCueLengths != null)
                        {
                            foreach (var cue in updatedCueLengths)
                            {
                                if (cue.Key.CanResize)
                                {
                                    cue.Key.SetValue(CueControl.SampleLengthProperty, (uint) cue.Value);
                                }
                            }
                        }
                        break;
                    case CueMoveMode.Normal:
                        if (updatedCuePositions != null)
                        {
                            foreach (var updatedCuePosition in updatedCuePositions)
                                updatedCuePosition.Key.SetValue(CueControl.SampleProperty,
                                    (uint) updatedCuePosition.Value);
                        }
                        break;
                    case CueMoveMode.LeftGrip:
                        if (updatedCueLengths != null)
                        {
                            foreach (var cue in updatedCueLengths)
                            {
                                if (cue.Key.CanResize)
                                {
                                    cue.Key.SetValue(CueControl.SampleLengthProperty, (uint) cue.Value);
                                }
                            }
                        }
                        if (updatedCuePositions != null)
                        {
                            foreach (var cue in updatedCuePositions)
                            {
                                if (cue.Key.CanResize)
                                {
                                    cue.Key.SetValue(CueControl.SampleProperty,
                                        (uint) cue.Value);
                                }
                            }
                        }
                        break;
                    case CueMoveMode.LeadIn:
                        if (updatedCueLeadInLengths != null)
                            foreach (var cue in updatedCueLeadInLengths)
                            {
                                if (cue.Key.CanResize)
                                {
                                    cue.Key.SetValue(EventCueControl.LeadInSampleLengthProperty, (uint) cue.Value);
                                }
                            }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void SelectionCanvas_CueMoved(object sender, EventArgs e)
        {
            _moving = false;
        }

        private static void SelectionBrushChangedCallback(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs e)
        {
            var control = (SelectionCanvas) dependencyObject;
            control.SelectionBrushChangedCallback(e);
        }

        private void SelectionBrushChangedCallback(DependencyPropertyChangedEventArgs e)
        {
            _selectionRectangle.Stroke = (Brush) e.NewValue;
        }

        public event RoutedEventHandler CueSelected
        {
            add { AddHandler(CueSelectedEvent, value); }
            remove { RemoveHandler(CueSelectedEvent, value); }
        }

        public event EventHandler<CueMovingEventArgs> CueMoving
        {
            add { AddHandler(CueMovingEvent, value); }
            remove { RemoveHandler(CueMovingEvent, value); }
        }

        public event EventHandler<CueMoveEventArgs> CueMove
        {
            add { AddHandler(CueMoveEvent, value); }
            remove { RemoveHandler(CueMoveEvent, value); }
        }

        public event EventHandler CueMoved
        {
            add { AddHandler(CueMovedEvent, value); }
            remove { RemoveHandler(CueMovedEvent, value); }
        }

        public event RoutedEventHandler TrackSelected
        {
            add { AddHandler(TrackSelectedEvent, value); }
            remove { RemoveHandler(TrackSelectedEvent, value); }
        }

        private void SelectionCanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.Focus(this);

            _clickPosition = e.GetPosition(this);

            CaptureMouse();

            Dictionary<EventCueControl, Rect> cueControlBounds = Cues.ToDictionary(
                control => control,
                control =>
                {
                    Point position = control.TransformToAncestor(this).Transform(new Point(0, 0));
                    return new Rect(position, new Size(control.ActualWidth, control.ActualHeight));
                });

            _cueControlBounds = new ReadOnlyDictionary<EventCueControl, Rect>(cueControlBounds);

            _selectionRectangle.SetValue(LeftProperty, _clickPosition.X);
            _selectionRectangle.SetValue(TopProperty, _clickPosition.Y);
            _selectionRectangle.Width = 0;
            _selectionRectangle.Height = 0;
            _selectionRectangle.SetValue(ZIndexProperty, int.MaxValue);
            _selectionRectangle.Visibility = Visibility.Visible;

            ClearTrackSelection();

            _selecting = true;

            AutoScroll();
        }

        private void SelectionCanvasMouseMove(object sender, MouseEventArgs e)
        {
            if (_selecting)
            {
                Point position = e.GetPosition(this);

                if (position.X < 0)
                    position.X = 0;
                else if (position.X > ActualWidth)
                    position.X = ActualWidth;

                if (position.Y < 0)
                    position.Y = 0;
                else if (position.Y > ActualHeight)
                    position.Y = ActualHeight;

                double dx = position.X - _clickPosition.X;
                double dy = position.Y - _clickPosition.Y;

                var selectionRect = new Rect();

                if (dx > 0)
                {
                    _selectionRectangle.SetValue(LeftProperty, _clickPosition.X);
                    _selectionRectangle.Width = dx;

                    selectionRect.X = _clickPosition.X;
                    selectionRect.Width = dx;
                }
                else
                {
                    _selectionRectangle.SetValue(LeftProperty, position.X);
                    _selectionRectangle.Width = -dx;

                    selectionRect.X = position.X;
                    selectionRect.Width = -dx;
                }

                if (dy > 0)
                {
                    _selectionRectangle.SetValue(TopProperty, _clickPosition.Y);
                    _selectionRectangle.Height = dy;

                    selectionRect.Y = _clickPosition.Y;
                    selectionRect.Height = dy;
                }
                else
                {
                    _selectionRectangle.SetValue(TopProperty, position.Y);
                    _selectionRectangle.Height = -dy;

                    selectionRect.Y = position.Y;
                    selectionRect.Height = -dy;
                }

                foreach (var cueControlBound in _cueControlBounds)
                {
                    cueControlBound.Key.IsSelected =
                        cueControlBound.Value.IntersectsWith(selectionRect);
                }
            }
        }

        private void SelectionCanvasMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ReleaseMouseCapture();
            SetCueSelection();
            _selectionRectangle.Visibility = Visibility.Hidden;
            _selecting = false;
        }

        private void SelectionCanvasCueSelected(object sender, RoutedEventArgs e)
        {
            foreach (TrackHeaderControl track in Tracks)
                track.IsSelected = false;

            var selectedCue = (EventCueControl) e.OriginalSource;

            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
                ClearCueSelection();

            selectedCue.IsSelected = true;
            SelectedCue = selectedCue;
            SetCueSelection();
        }

        private void SelectionCanvasTrackSelected(object sender, RoutedEventArgs e)
        {
            foreach (EventCueControl eventCue in Cues)
                eventCue.IsSelected = false;

            SelectedCue = null;

            var selectedTrack = (TrackHeaderControl) e.OriginalSource;

            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
                ClearTrackSelection();

            selectedTrack.IsSelected = true;
            SelectedTracks = GetSelectedTracks().Select(track => track.DataContext).ToList();
            SelectedItems = SelectedTracks;

            SelectedItem = SelectedItems.Count == 1 ? SelectedItems[0] : null;
        }

        private void SelectionCanvasKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && DeleteCommand != null)
                DeleteCommand.Execute(null);

            if (e.Key == Key.Space)
                AddCueCommand.Execute(null);
        }

        private void SetCueSelection()
        {
            SelectedCues = GetSelectedCues().Select(cue => cue.DataContext).ToList();

            if (SelectedCues.Count == 0)
                SelectedCue = null;

            SelectedItems = SelectedCues;
            SelectedItem = SelectedItems.Count == 1 ? SelectedItems[0] : null;
            SelectedTracks = null;
        }

        private void ClearCueSelection()
        {
            foreach (EventCueControl eventCue in Cues)
                eventCue.IsSelected = false;
            SelectedCue = null;
        }

        private void ClearTrackSelection()
        {
            foreach (TrackHeaderControl track in Tracks)
                track.IsSelected = false;
        }

        private async void AutoScroll()
        {
            if (_scrollViewer == null)
                _scrollViewer = FindAncestor<ScrollViewer>();

            if (_scrollViewer == null)
                return;

            while (_selecting)
            {
                Point mousePosition = Mouse.GetPosition(this);

                double canvasRight = _scrollViewer.HorizontalOffset + _scrollViewer.ViewportWidth;
                double mouseRight = canvasRight - mousePosition.X;


                if (mouseRight < AutoscrollDistance)
                {
                    double delta = AutoscrollDistance - mouseRight;
                    _scrollViewer.ScrollToHorizontalOffset(_scrollViewer.HorizontalOffset + delta);
                }
                else
                {
                    double mouseLeft = mousePosition.X - _scrollViewer.HorizontalOffset;

                    if (mouseLeft < AutoscrollDistance)
                    {
                        double delta = mouseLeft - AutoscrollDistance;
                        _scrollViewer.ScrollToHorizontalOffset(_scrollViewer.HorizontalOffset + delta);
                    }
                }

                double canvasBottom = _scrollViewer.VerticalOffset + _scrollViewer.ViewportHeight;
                double mouseBottom = canvasBottom - mousePosition.Y;

                if (mouseBottom < AutoscrollDistance)
                {
                    double delta = AutoscrollDistance - mouseBottom;
                    _scrollViewer.ScrollToVerticalOffset(_scrollViewer.VerticalOffset + delta);
                }
                else
                {
                    double mouseTop = mousePosition.Y - _scrollViewer.VerticalOffset;

                    if (mouseTop < AutoscrollDistance)
                    {
                        double delta = mouseTop - AutoscrollDistance;
                        _scrollViewer.ScrollToVerticalOffset(_scrollViewer.VerticalOffset + delta);
                    }
                }

                await Task.Delay(10);
            }
        }

        private T FindAncestor<T>() where T : DependencyObject
        {
            return UiHelper.FindAncestor<T>(this);
        }

        public IEnumerable<T> FindVisualChildren<T>() where T : DependencyObject
        {
            return UiHelper.FindVisualChildren<T>(this);
        }
    }
}