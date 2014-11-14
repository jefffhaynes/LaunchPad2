using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SC18IM700.Commands
{
    public class RegisterWrite : CommandBase
    {
        public RegisterWrite() : base('W') { }

        public RegisterWrite(Register register)
            : this()
        {
            Register = register;
        }

        public RegisterWrite(Register register, byte[] data)
            : this(register)
        {
            Data = data;
        }

        public Register Register { get; set; }
        public byte[] Data { get; set; }

        public override byte[] GetPacket()
        {
            MemoryStream packet = new MemoryStream();
            packet.WriteByte(Command);
            packet.WriteByte((byte)Register);
            packet.Write(Data, 0, Data.Length);
            packet.WriteByte(EOL);
            return packet.ToArray();
        }
    }
}
