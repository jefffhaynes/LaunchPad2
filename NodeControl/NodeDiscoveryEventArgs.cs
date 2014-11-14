using System;

namespace NodeControl
{
    public class NodeDiscoveryEventArgs : EventArgs
    {
        public NodeDiscoveryEventArgs(Node node)
        {
            Node = node;
        }

        public Node Node { get; private set; }
    }
}
