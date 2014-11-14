using System;
using System.IO;

namespace XBee.Frames
{
    public class TransmitDataRequest : XBeeFrame
    {
        [Flags]
        public enum OptionValues : byte
        {
            DisableAck = 0x01,
            EnableApsEncryption = 0x20,
            ExtendedTimeout = 0x40
        }

        private readonly XBeeNode _destination;
        private byte[] _data;

        public TransmitDataRequest(XBeeNode destination)
        {
            CommandId = XBeeAPICommandId.TRANSMIT_DATA_REQUEST;
            BroadcastRadius = 0;
            Options = 0;

            _destination = destination;
        }

        public byte BroadcastRadius { get; set; }

        public OptionValues Options { get; set; }

        public void SetData(byte[] data)
        {
            _data = data;
        }

        public override byte[] ToByteArray()
        {
            var stream = new MemoryStream();

            stream.WriteByte((byte) CommandId);
            stream.WriteByte(FrameId);

            byte[] addressData = _destination.Address64.GetAddress();
            stream.Write(addressData, 0, 8);
            stream.Write(_destination.Address16.GetAddress(), 0, 2);

            //stream.WriteByte(BroadcastRadius);
            stream.WriteByte((byte) Options);

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