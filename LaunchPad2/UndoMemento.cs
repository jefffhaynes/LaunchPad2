using System;

namespace LaunchPad2
{
    public class UndoMemento : IUndoMemento
    {
        private readonly Action _doAction;
        private readonly Action _undoAction;

        public UndoMemento(Action doAction, Action undoAction)
        {
            if (doAction == null)
                throw new ArgumentNullException(nameof(doAction));

            if (undoAction == null)
                throw new ArgumentNullException(nameof(undoAction));

            _doAction = doAction;
            _undoAction = undoAction;
        }

        public void Do()
        {
            _doAction();
        }

        public void Undo()
        {
            _undoAction();
        }
    }
}