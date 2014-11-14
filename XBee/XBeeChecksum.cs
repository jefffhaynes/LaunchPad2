using System;
using System.Linq;

namespace XBee
{
    public class XBeeChecksum
    {
        public static byte Calculate(byte[] data)
        {
            var checksum = data.Aggregate(0, (current, b) => current + b);

            // discard values > 1 byte
            checksum = 0xff & checksum;
            // perform 2s complement
            checksum = 0xff - checksum;

            return (byte)checksum;
        }

        public static bool Verify(byte[] data)
        {
            int checksum = Calculate(data);
            checksum = checksum & 0xff;

            return checksum == 0x00;
        }
    }
}
