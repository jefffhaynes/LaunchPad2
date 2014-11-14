using System;

namespace XBee.Frames
{
    public class TxStatusResponse : XBeeFrame
    {
        private readonly PacketParser _parser;

        public TxStatusResponse(PacketParser parser)
        {
            _parser = parser;
            CommandId = XBeeAPICommandId.TX_STATUS_RESPONSE;
        }

        public override byte[] ToByteArray()
        {
            throw new NotSupportedException();
        }

        public byte Status { get; private set; }

        public override void Parse()
        {
            FrameId = (byte) _parser.ReadByte();
            Status = (byte) _parser.ReadByte();
        }
    }
}
