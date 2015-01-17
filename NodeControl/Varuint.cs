using System;
using System.IO;
using BinarySerialization;

namespace NodeControl
{
    public class Varuint : IBinarySerializable
    {
        [Ignore]
        public uint Value { get; set; }

        public void Deserialize(Stream stream, Endianness endianness, BinarySerializationContext context)
        {
            var reader = new StreamReader(stream);

            bool more = true;
            int shift = 0;

            Value = 0;

            while (more)
            {
                int b = reader.Read();

                if (b == -1)
                    throw new InvalidOperationException("Reached end of stream before end of varuint.");

                var lower7Bits = (byte) b;
                more = (lower7Bits & 128) != 0;
                Value |= (uint) ((lower7Bits & 0x7f) << shift);
                shift += 7;
            }
        }

        public void Serialize(Stream stream, Endianness endianness, BinarySerializationContext context)
        {
            var writer = new StreamWriter(stream);
            
                bool first = true;
                while (first || Value > 0)
                {
                    first = false;
                    var lower7Bits = (byte)(Value & 0x7f);
                    Value >>= 7;
                    if (Value > 0)
                        lower7Bits |= 128;
                    writer.Write(lower7Bits);
                }
            
        }
    }
}
