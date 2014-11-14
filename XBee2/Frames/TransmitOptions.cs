using System;

namespace XBee2.Frames
{
    [Flags]
    public enum TransmitOptions : byte
    {
        DisableAck = 0x1,
        BroadcastPanId = 0x4,
    }
}
