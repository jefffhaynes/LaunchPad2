﻿namespace XBee2.Frames
{
    public class TxStatusFrame : CommandResponseFrameContent
    {
        public DeliveryStatus Status { get; set; }
    }
}
