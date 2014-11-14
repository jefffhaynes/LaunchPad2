using System.Collections.Generic;
using BinarySerialization;

namespace NodeControl
{
    public class StatusCommand : ControlObjectPayload
    {
        public byte PortCount { get; set; }

        [FieldLength("PortCount")]
        public List<PortStatus> PortStatus { get; set; }

        public bool IsArmedLocally { get; set; }

        public bool IsArmedRemotely { get; set; }

        public BatteryState BatteryState { get; set; }
    }
}
