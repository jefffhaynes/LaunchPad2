using System.Collections.Generic;
using BinarySerialization;

namespace NodeControl
{
    public class StatusCommand : ControlObjectPayload
    {
        [FieldOrder(0)]
        public byte PortCount { get; set; }

        [FieldOrder(1)]
        [FieldCount("PortCount")]
        public List<PortStatus> PortStatus { get; set; }

        [FieldOrder(2)]
        public bool IsArmedLocally { get; set; }

        [FieldOrder(3)]
        public bool IsArmedRemotely { get; set; }

        [FieldOrder(4)]
        public BatteryState BatteryState { get; set; }
    }
}
