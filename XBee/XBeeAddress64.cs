using System;
using System.Linq;

namespace XBee
{
    public class XBeeAddress64 : XBeeAddress
    {
        public static readonly XBeeAddress64 Broadcast = new XBeeAddress64(0x000000000000FFFF);
        public static readonly XBeeAddress64 Coordinator = new XBeeAddress64(0);
        public static readonly XBeeAddress64 ZnetCoordinator = new XBeeAddress64(0);

        public XBeeAddress64(ulong address)
        {
            Address = address;
        }

        public ulong Address { get; private set; }

        public override byte[] GetAddress()
        {
            var addressLittleEndian = BitConverter.GetBytes(Address);
            Array.Reverse(addressLittleEndian);
            return addressLittleEndian;
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
                return true;

            if ((obj == null) || (typeof(XBeeAddress64) != obj.GetType()))
                return false;

            var addr = (XBeeAddress64) obj;

            return GetAddress().SequenceEqual(addr.GetAddress());
        }

        public bool IsCoordinator
        {
            get { return Equals(Coordinator); }
        }

        public override int GetHashCode()
        {
            return Address.GetHashCode();
        }
    }
}
