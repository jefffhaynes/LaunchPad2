using BinarySerialization;

namespace NodeControl
{
    public class PortStatus
    {
        [FieldOrder(0)]
        public byte PortId { get; set; }

        [FieldOrder(1)]
        public PortState State { get; set; }
    }
}
