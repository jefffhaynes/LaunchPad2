using System;
using System.IO;

namespace XBee
{
    public class EscapedPacketReader : PacketReader
    {
        protected override void ProcessReceivedData()
        {
            Stream = EscapeData(Stream.ToArray());
            base.ProcessReceivedData();
        }

        public MemoryStream EscapeData(byte[] data)
        {
            var escapeNext = false;
            var escapedData = new MemoryStream();
            foreach (var b in data) {
                if (IsSpecialByte(b)) {
                    if (b == (byte) XBeeSpecialBytes.EscapeByte) {
                        escapeNext = true;
                        continue;
                    }
                    if (b == (byte) XBeeSpecialBytes.StartByte) {
                        continue;
                    }
                }

                if (escapeNext) {
                    escapedData.WriteByte(EscapeByte(b));
                    escapeNext = false;
                } else {
                    escapedData.WriteByte(b);
                }
            }
            return escapedData;
        }

        public bool IsSpecialByte(byte b)
        {
            return Enum.IsDefined(typeof(XBeeSpecialBytes), b);
        }

        public byte EscapeByte(byte b)
        {
            return (byte) (0x20 ^ b);
        }
    }
}