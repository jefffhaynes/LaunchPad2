using System;
using System.Collections.Generic;
using System.Linq;

namespace LaunchPad2
{
    public class ActionBatch
    {
        private readonly List<Action> _actions = new List<Action>();

        public void Add(Action action)
        {
            _actions.Add(action);
        }

        public Action BatchAction
        {
            get
            {
                return () =>
                {
                    foreach (var action in _actions)
                        action.Invoke();
                };
            }
        }

        public Action ReverseBatchAction
        {
            get
            {
                return () =>
                {
                    foreach (var action in Enumerable.Reverse(_actions))
                        action.Invoke();
                };
            }
        }
    }
}
