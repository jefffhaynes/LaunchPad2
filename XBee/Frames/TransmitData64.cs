using System;
using System.IO;

namespace XBee.Frames
{
    public class TransmitData64 : XBeeFrame
    {
        [Flags]
        private enum Options : byte
        {
            None = 0x0,
            DisableAck = 0x1
        }

        public TransmitData64(XBeeAddress64 destination, byte[] data, bool disableAck = false)
        {
            _destination = destination;
            _data = data;
            DisableAck = disableAck;
        }

        public bool DisableAck { get; set; }

        private readonly XBeeAddress64 _destination;
        private readonly byte[] _data;

        public override byte[] ToByteArray()
        {
            var stream = new MemoryStream();

            stream.WriteByte((byte)XBeeAPICommandId.REQUEST_64);
            stream.WriteByte(FrameId);

            byte[] addressData = _destination.GetAddress();
            stream.Write(addressData, 0, addressData.Length);

            var options = DisableAck ? Options.DisableAck : Options.None;
            stream.WriteByte((byte)options);

            if (_data != null)
            {
                stream.Write(_data, 0, _data.Length);
            }

            return stream.ToArray();
        }

        public override void Parse()
        {
            throw new NotImplementedException();
        }
    }
}
