using XBee2;

namespace NodeControl
{
    public class Node
    {
        public Node(string name, LongAddress address, ConnectionQuality connectionQuality)
        {
            Name = name;
            Address = address;
            ConnectionQuality = connectionQuality;
        }

        public string Name { get; private set; }

        public LongAddress Address { get; private set; }

        public ConnectionQuality ConnectionQuality { get; private set; }
    }
}
