using System;
using System.Collections.Generic;

namespace LaunchPad2
{
    public static class UndoManager
    {
        private const int UndoDepth = 64;

        private static readonly LinkedList<IUndoMemento> UndoMementos = new LinkedList<IUndoMemento>();

        private static LinkedListNode<IUndoMemento> _current;

        private static readonly object UndoLock = new object();

        public static bool CanUndo
        {
            get { return _current != null; }
        }

        public static bool CanRedo
        {
            get { return UndoMementos.First != UndoMementos.Last; }
        }

        public static event EventHandler CanUndoChanged;

        public static event EventHandler CanRedoChanged;

        public static void DoAndAdd(IUndoMemento undoMemento)
        {
            undoMemento.Do();
            Add(undoMemento);
        }

        public static void Add(IUndoMemento undoMemento)
        {
            lock (UndoLock)
            {
                _current = _current == null
                    ? UndoMementos.AddFirst(undoMemento)
                    : UndoMementos.AddAfter(_current, undoMemento);

                while (UndoMementos.Last != _current)
                    UndoMementos.RemoveLast();

                if (UndoMementos.Count > UndoDepth)
                    UndoMementos.RemoveFirst();

                OnUndoRedoChanged();
            }
        }

        public static void DoAndAdd(Action doAction, Action undoAction)
        {
            DoAndAdd(new UndoMemento(doAction, undoAction));
        }

        public static void Add(Action doAction, Action undoAction)
        {
            Add(new UndoMemento(doAction, undoAction));
        }

        public static void Undo()
        {
            lock (UndoLock)
            {
                if (!CanUndo)
                    return;

                _current.Value.Undo();
                _current = _current.Previous;

                OnUndoRedoChanged();
            }
        }

        public static void Redo()
        {
            lock (UndoLock)
            {
                if (!CanRedo)
                    return;

                if (_current == null)
                    _current = UndoMementos.First;
                else
                {
                    if (_current.Next == null)
                        return;

                    _current = _current.Next;
                }

                _current.Value.Do();

                OnUndoRedoChanged();
            }
        }

        private static void OnUndoRedoChanged()
        {
            if (CanUndoChanged != null)
                CanUndoChanged(null, EventArgs.Empty);

            if (CanRedoChanged != null)
                CanRedoChanged(null, EventArgs.Empty);
        }
    }
}