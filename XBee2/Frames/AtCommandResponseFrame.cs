﻿using BinarySerialization;
using XBee2.Frames.AtCommands;

namespace XBee2.Frames
{
    public class AtCommandResponseFrame<TResponseData> : CommandResponseFrameContent 
        where TResponseData : AtCommandResponseFrameData
    {
        private const int AtCommandFieldLength = 2;

        [FieldLength(AtCommandFieldLength)]
        public string AtCommand { get; set; }

        public AtCommandStatus Status { get; set; }

        [Subtype("AtCommand", "ND", typeof(NetworkDiscoveryResponseData))]
        [Subtype("AtCommand", "HV", typeof(HardwareVersionResponseData))]
        public TResponseData Data { get; set; }
    }

    public class AtCommandResponseFrame : AtCommandResponseFrame<AtCommandResponseFrameData>
    {
    }
}
