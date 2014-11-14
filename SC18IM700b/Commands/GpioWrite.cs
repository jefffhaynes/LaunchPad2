using System.IO;

namespace SC18IM700.Commands
{
    public class GpioWrite : CommandBase
    {
        public GpioWrite() : base('O') { }

        public GpioWrite(GpioPorts hiPorts) : this()
        {
            HiPorts = hiPorts;
        }

        public GpioPorts HiPorts { get; set; }

        public override byte[] GetPacket()
        {
            var packet = new MemoryStream();
            packet.WriteByte(Command);
            packet.WriteByte((byte)HiPorts);
            packet.WriteByte(EOL);
            return packet.ToArray();
        }
    }
}
