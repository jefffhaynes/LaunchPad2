using System;

namespace LaunchPad2
{
    public class UndoBatchMemento : IUndoMemento
    {
        private readonly ActionBatch _doBatch = new ActionBatch();

        private readonly ActionBatch _undoBatch = new ActionBatch();

        public void Do()
        {
            _doBatch.BatchAction();
        }

        public void Undo()
        {
            _undoBatch.ReverseBatchAction();
        }

        public void Add(Action doAction, Action undoAction)
        {
            if(doAction == null)
                throw new ArgumentNullException(nameof(doAction));

            if(undoAction == null)
                throw new ArgumentNullException(nameof(undoAction));

            _doBatch.Add(doAction);
            _undoBatch.Add(undoAction);
        }
    }
}