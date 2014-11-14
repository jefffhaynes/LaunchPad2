
namespace XBee
{
    public class XBeeNodeInfo
    {
        public XBeeNodeInfo(XBeeNode node, string name, XBeeNodeSignalStrength signalStrength)
        {
            Node = node;
            Name = name;
            SignalStrength = signalStrength;
        }

        public XBeeNode Node { get; private set; }

        public string Name { get; private set; }

        public XBeeNodeSignalStrength SignalStrength { get; private set; }

        public bool IsCoordinator
        {
            get { return Node.Address64.IsCoordinator; }
        }
    }
}
