using System;

namespace SC18IM700
{
    [Flags]
    public enum GpioPorts : byte
    {
        None = 0x0,
        Port0 = 0x1,
        Port1 = 0x2,
        Port2 = 0x4,
        Port3 = 0x8,
        Port4 = 0x10,
        Port5 = 0x20,
        Port6 = 0x40,
        Port7 = 0x80,
        All = Port0 | Port1 | Port2 | Port3 | Port4 | Port5 | Port6 | Port7,
        Conf1Ports = Port0 | Port1 | Port2 | Port3,
        Conf2Ports = Port4 | Port5 | Port6 | Port7
    }
}
