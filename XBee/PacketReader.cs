using System.IO;
using System.Linq;
using XBee.Exceptions;

namespace XBee
{
    public class PacketReader : IPacketReader
    {
        public event FrameReceivedHandler FrameReceived;

        protected MemoryStream Stream = new MemoryStream();
        private uint _packetLength;

        public void ReceiveData(byte[] data)
        {
            if (data.Length == 0)
                return;

            if (_packetLength == 0 && data[0] == (byte) XBeeSpecialBytes.StartByte)
            {
                Stream = new MemoryStream();
                _packetLength = 0;
            }

            CopyAndProcessData(data);
        }

        private void CopyAndProcessData(byte[] data)
        {
            foreach (var b in data.Where(b => Stream.Length != 0 || b != (byte) XBeeSpecialBytes.StartByte))
            {
                Stream.WriteByte(b);
            }

            if (_packetLength == 0 && Stream.Length > 2)
            {
                var packet = Stream.ToArray();
                _packetLength = (uint) (packet[0] << 8 | packet[1]) + 3;
            }

            if (Stream.Length < 3)
                return;

            if (_packetLength != 0 && Stream.Length < _packetLength)
                return;

            ProcessReceivedData();
        }

        protected virtual void ProcessReceivedData()
        {
            try
            {
                var frame = XBeePacketUnmarshaler.Unmarshal(Stream.ToArray());
                _packetLength = 0;
                if (FrameReceived != null)
                    FrameReceived.Invoke(this, new FrameReceivedArgs(frame));
            }
            catch (XBeeFrameException ex)
            {
                throw new XBeeException("Unable to unmarshal packet.", ex);
            }
        }
    }
}
