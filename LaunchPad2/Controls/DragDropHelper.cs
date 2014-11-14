using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace LaunchPad2.Controls
{
    public class DragDropHelper
    {
        // source and target
        private static DragDropHelper _instance;

        public static readonly DependencyProperty IsDragSourceProperty =
            DependencyProperty.RegisterAttached("IsDragSource", typeof (bool), typeof (DragDropHelper),
                new UIPropertyMetadata(false, IsDragSourceChanged));

        public static readonly DependencyProperty IsDropTargetProperty =
            DependencyProperty.RegisterAttached("IsDropTarget", typeof (bool), typeof (DragDropHelper),
                new UIPropertyMetadata(false, IsDropTargetChanged));

        public static readonly DependencyProperty DragDropTemplateProperty =
            DependencyProperty.RegisterAttached("DragDropTemplate", typeof (DataTemplate), typeof (DragDropHelper),
                new UIPropertyMetadata(null));

        public static readonly DependencyProperty DragDropGripNameProperty =
    DependencyProperty.RegisterAttached("DragDropGripName", typeof(string), typeof(DragDropHelper),
        new UIPropertyMetadata(null));

        private readonly DataFormat _format = DataFormats.GetDataFormat("DragDropItemsControl");
        private DraggedAdorner _draggedAdorner;
        private object _draggedData;
        private bool _hasVerticalOrientation;
        private Vector _initialMouseOffset;
        private Point _initialMousePosition;
        private InsertionAdorner _insertionAdorner;
        private int _insertionIndex;
        private bool _isInFirstHalf;
        private FrameworkElement _sourceItemContainer;
        private ItemsControl _sourceItemsControl;
        private FrameworkElement _targetItemContainer;
        private ItemsControl _targetItemsControl;
        private Window _topWindow;

        // singleton

        private static DragDropHelper Instance
        {
            get { return _instance ?? (_instance = new DragDropHelper()); }
        }

        public static bool GetIsDragSource(DependencyObject obj)
        {
            return (bool) obj.GetValue(IsDragSourceProperty);
        }

        public static void SetIsDragSource(DependencyObject obj, bool value)
        {
            obj.SetValue(IsDragSourceProperty, value);
        }


        public static bool GetIsDropTarget(DependencyObject obj)
        {
            return (bool) obj.GetValue(IsDropTargetProperty);
        }

        public static void SetIsDropTarget(DependencyObject obj, bool value)
        {
            obj.SetValue(IsDropTargetProperty, value);
        }

        public static DataTemplate GetDragDropTemplate(DependencyObject obj)
        {
            return (DataTemplate) obj.GetValue(DragDropTemplateProperty);
        }

        public static void SetDragDropGripName(DependencyObject obj, string value)
        {
            obj.SetValue(DragDropGripNameProperty, value);
        }
        public static string GetDragDropGripName(DependencyObject obj)
        {
            return (string)obj.GetValue(DragDropGripNameProperty);
        }

        public static void SetDragDropTemplate(DependencyObject obj, DataTemplate value)
        {
            obj.SetValue(DragDropTemplateProperty, value);
        }

        private static void IsDragSourceChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var dragSource = obj as ItemsControl;
            if (dragSource != null)
            {
                if (Equals(e.NewValue, true))
                {
                    dragSource.PreviewMouseLeftButtonDown += Instance.DragSource_MouseLeftButtonDown;
                    dragSource.PreviewMouseLeftButtonUp += Instance.DragSource_MouseLeftButtonUp;
                    dragSource.PreviewMouseMove += Instance.DragSource_MouseMove;
                }
                else
                {
                    dragSource.PreviewMouseLeftButtonDown -= Instance.DragSource_MouseLeftButtonDown;
                    dragSource.PreviewMouseLeftButtonUp -= Instance.DragSource_MouseLeftButtonUp;
                    dragSource.PreviewMouseMove -= Instance.DragSource_MouseMove;
                }
            }
        }

        private static void IsDropTargetChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var dropTarget = obj as ItemsControl;
            if (dropTarget != null)
            {
                if (Equals(e.NewValue, true))
                {
                    dropTarget.AllowDrop = true;
                    dropTarget.PreviewDrop += Instance.DropTarget_PreviewDrop;
                    dropTarget.PreviewDragEnter += Instance.DropTarget_PreviewDragEnter;
                    dropTarget.PreviewDragOver += Instance.DropTarget_PreviewDragOver;
                    dropTarget.PreviewDragLeave += Instance.DropTarget_PreviewDragLeave;
                }
                else
                {
                    dropTarget.AllowDrop = false;
                    dropTarget.PreviewDrop -= Instance.DropTarget_PreviewDrop;
                    dropTarget.PreviewDragEnter -= Instance.DropTarget_PreviewDragEnter;
                    dropTarget.PreviewDragOver -= Instance.DropTarget_PreviewDragOver;
                    dropTarget.PreviewDragLeave -= Instance.DropTarget_PreviewDragLeave;
                }
            }
        }

        // DragSource

        private void DragSource_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _sourceItemsControl = (ItemsControl) sender;
            var visual = (FrameworkElement)e.OriginalSource;

            var gripName = GetDragDropGripName(_sourceItemsControl);
            if (gripName != null && !string.Equals(visual.Name, gripName))
                return;
            
            _topWindow = Window.GetWindow(_sourceItemsControl);
            _initialMousePosition = e.GetPosition(_topWindow);

            _sourceItemContainer = _sourceItemsControl.ContainerFromElement(visual) as FrameworkElement;

            if (_sourceItemContainer != null)
            {
                _draggedData = _sourceItemContainer.DataContext;
            }
        }

        // Drag = mouse down + move by a certain amount
        private void DragSource_MouseMove(object sender, MouseEventArgs e)
        {
            if (_draggedData != null)
            {
                // Only drag when user moved the mouse by a reasonable amount.
                if (DragAndDropUtilities.IsMovementBigEnough(_initialMousePosition, e.GetPosition(_topWindow)))
                {
                    _initialMouseOffset = _initialMousePosition -
                                         _sourceItemContainer.TranslatePoint(new Point(0, 0), _topWindow);

                    var data = new DataObject(_format.Name, _draggedData);

                    // Adding events to the window to make sure dragged adorner comes up when mouse is not over a drop target.
                    bool previousAllowDrop = _topWindow.AllowDrop;
                    _topWindow.AllowDrop = true;
                    _topWindow.DragEnter += TopWindow_DragEnter;
                    _topWindow.DragOver += TopWindow_DragOver;
                    _topWindow.DragLeave += TopWindow_DragLeave;

                    DragDropEffects effects = DragDrop.DoDragDrop((DependencyObject) sender, data, DragDropEffects.Move);

                    // Without this call, there would be a bug in the following scenario: Click on a data item, and drag
                    // the mouse very fast outside of the window. When doing this really fast, for some reason I don't get 
                    // the Window leave event, and the dragged adorner is left behind.
                    // With this call, the dragged adorner will disappear when we release the mouse outside of the window,
                    // which is when the DoDragDrop synchronous method returns.
                    RemoveDraggedAdorner();

                    _topWindow.AllowDrop = previousAllowDrop;
                    _topWindow.DragEnter -= TopWindow_DragEnter;
                    _topWindow.DragOver -= TopWindow_DragOver;
                    _topWindow.DragLeave -= TopWindow_DragLeave;

                    _draggedData = null;
                }
            }
        }

        private void DragSource_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _draggedData = null;
        }

        // DropTarget

        private void DropTarget_PreviewDragEnter(object sender, DragEventArgs e)
        {
            _targetItemsControl = (ItemsControl) sender;
            object draggedItem = e.Data.GetData(_format.Name);

            DecideDropTarget(e);
            if (draggedItem != null)
            {
                // Dragged Adorner is created on the first enter only.
                ShowDraggedAdorner(e.GetPosition(_topWindow));
                CreateInsertionAdorner();
            }
            e.Handled = true;
        }

        private void DropTarget_PreviewDragOver(object sender, DragEventArgs e)
        {
            object draggedItem = e.Data.GetData(_format.Name);

            DecideDropTarget(e);
            if (draggedItem != null)
            {
                // Dragged Adorner is only updated here - it has already been created in DragEnter.
                ShowDraggedAdorner(e.GetPosition(_topWindow));
                UpdateInsertionAdornerPosition();
            }
            e.Handled = true;
        }

        private void DropTarget_PreviewDrop(object sender, DragEventArgs e)
        {
            object draggedItem = e.Data.GetData(_format.Name);
            int indexRemoved = -1;

            if (draggedItem != null)
            {
                if ((e.Effects & DragDropEffects.Move) != 0)
                {
                    indexRemoved = DragAndDropUtilities.RemoveItemFromItemsControl(_sourceItemsControl, draggedItem);
                }
                // This happens when we drag an item to a later position within the same ItemsControl.
                if (indexRemoved != -1 && Equals(_sourceItemsControl, _targetItemsControl) && indexRemoved < _insertionIndex)
                {
                    _insertionIndex--;
                }
                DragAndDropUtilities.InsertItemInItemsControl(_targetItemsControl, draggedItem, _insertionIndex);

                RemoveDraggedAdorner();
                RemoveInsertionAdorner();

                var sourceItemsSource = _sourceItemsControl.ItemsSource as IList;
                var targetItemsSource = _targetItemsControl.ItemsSource as IList;

                if (sourceItemsSource != null && targetItemsSource != null)
                {
                    var doAction = new Action(() =>
                    {
                        sourceItemsSource.Remove(draggedItem);
                        targetItemsSource.Insert(_insertionIndex, draggedItem);
                    });

                    var undoAction = new Action(() =>
                    {
                        targetItemsSource.Remove(draggedItem);
                        sourceItemsSource.Insert(indexRemoved, draggedItem);
                    });

                    UndoManager.Add(doAction, undoAction);
                }
            }
            e.Handled = true;
        }

        private void DropTarget_PreviewDragLeave(object sender, DragEventArgs e)
        {
            // Dragged Adorner is only created once on DragEnter + every time we enter the window. 
            // It's only removed once on the DragDrop, and every time we leave the window. (so no need to remove it here)
            object draggedItem = e.Data.GetData(_format.Name);

            if (draggedItem != null)
            {
                RemoveInsertionAdorner();
            }
            e.Handled = true;
        }

        // If the types of the dragged data and ItemsControl's source are compatible, 
        // there are 3 situations to have into account when deciding the drop target:
        // 1. mouse is over an items container
        // 2. mouse is over the empty part of an ItemsControl, but ItemsControl is not empty
        // 3. mouse is over an empty ItemsControl.
        // The goal of this method is to decide on the values of the following properties: 
        // targetItemContainer, insertionIndex and isInFirstHalf.
        private void DecideDropTarget(DragEventArgs e)
        {
            int targetItemsControlCount = _targetItemsControl.Items.Count;
            object draggedItem = e.Data.GetData(_format.Name);

            if (IsDropDataTypeAllowed(draggedItem))
            {
                if (targetItemsControlCount > 0)
                {
                    _hasVerticalOrientation =
                        DragAndDropUtilities.HasVerticalOrientation(
                            _targetItemsControl.ItemContainerGenerator.ContainerFromIndex(0) as FrameworkElement);
                    _targetItemContainer =
                        _targetItemsControl.ContainerFromElement((DependencyObject) e.OriginalSource) as FrameworkElement;

                    if (_targetItemContainer != null)
                    {
                        Point positionRelativeToItemContainer = e.GetPosition(_targetItemContainer);
                        _isInFirstHalf = DragAndDropUtilities.IsInFirstHalf(_targetItemContainer,
                            positionRelativeToItemContainer, _hasVerticalOrientation);
                        _insertionIndex =
                            _targetItemsControl.ItemContainerGenerator.IndexFromContainer(_targetItemContainer);

                        if (!_isInFirstHalf)
                        {
                            _insertionIndex++;
                        }
                    }
                    else
                    {
                        _targetItemContainer =
                            _targetItemsControl.ItemContainerGenerator.ContainerFromIndex(targetItemsControlCount - 1) as
                                FrameworkElement;
                        _isInFirstHalf = false;
                        _insertionIndex = targetItemsControlCount;
                    }
                }
                else
                {
                    _targetItemContainer = null;
                    _insertionIndex = 0;
                }
            }
            else
            {
                _targetItemContainer = null;
                _insertionIndex = -1;
                e.Effects = DragDropEffects.None;
            }
        }

        // Can the dragged data be added to the destination collection?
        // It can if destination is bound to IList<allowed type>, IList or not data bound.
        private bool IsDropDataTypeAllowed(object draggedItem)
        {
            bool isDropDataTypeAllowed;
            IEnumerable collectionSource = _targetItemsControl.ItemsSource;
            if (draggedItem != null)
            {
                if (collectionSource != null)
                {
                    Type draggedType = draggedItem.GetType();
                    Type collectionType = collectionSource.GetType();

                    Type genericIListType = collectionType.GetInterface("IList`1");
                    if (genericIListType != null)
                    {
                        Type[] genericArguments = genericIListType.GetGenericArguments();
                        isDropDataTypeAllowed = genericArguments[0].IsAssignableFrom(draggedType);
                    }
                    else if (typeof (IList).IsAssignableFrom(collectionType))
                    {
                        isDropDataTypeAllowed = true;
                    }
                    else
                    {
                        isDropDataTypeAllowed = false;
                    }
                }
                else // the ItemsControl's ItemsSource is not data bound.
                {
                    isDropDataTypeAllowed = true;
                }
            }
            else
            {
                isDropDataTypeAllowed = false;
            }
            return isDropDataTypeAllowed;
        }

        // Window

        private void TopWindow_DragEnter(object sender, DragEventArgs e)
        {
            ShowDraggedAdorner(e.GetPosition(_topWindow));
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void TopWindow_DragOver(object sender, DragEventArgs e)
        {
            ShowDraggedAdorner(e.GetPosition(_topWindow));
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void TopWindow_DragLeave(object sender, DragEventArgs e)
        {
            RemoveDraggedAdorner();
            e.Handled = true;
        }

        // Adorners

        // Creates or updates the dragged Adorner. 
        private void ShowDraggedAdorner(Point currentPosition)
        {
            if (_draggedAdorner == null)
            {
                AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(_sourceItemsControl);
                _draggedAdorner = new DraggedAdorner(_draggedData, GetDragDropTemplate(_sourceItemsControl),
                    _sourceItemContainer, adornerLayer);
            }
            _draggedAdorner.SetPosition(currentPosition.X - _initialMousePosition.X + _initialMouseOffset.X,
                currentPosition.Y - _initialMousePosition.Y + _initialMouseOffset.Y);
        }

        private void RemoveDraggedAdorner()
        {
            if (_draggedAdorner != null)
            {
                _draggedAdorner.Detach();
                _draggedAdorner = null;
            }
        }

        private void CreateInsertionAdorner()
        {
            if (_targetItemContainer != null)
            {
                // Here, I need to get adorner layer from targetItemContainer and not targetItemsControl. 
                // This way I get the AdornerLayer within ScrollContentPresenter, and not the one under AdornerDecorator (Snoop is awesome).
                // If I used targetItemsControl, the adorner would hang out of ItemsControl when there's a horizontal scroll bar.
                AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(_targetItemContainer);
                _insertionAdorner = new InsertionAdorner(_hasVerticalOrientation, _isInFirstHalf, _targetItemContainer,
                    adornerLayer);
            }
        }

        private void UpdateInsertionAdornerPosition()
        {
            if (_insertionAdorner != null)
            {
                _insertionAdorner.IsInFirstHalf = _isInFirstHalf;
                _insertionAdorner.InvalidateVisual();
            }
        }

        private void RemoveInsertionAdorner()
        {
            if (_insertionAdorner != null)
            {
                _insertionAdorner.Detach();
                _insertionAdorner = null;
            }
        }
    }
}