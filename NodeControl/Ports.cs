using System;
using System.ComponentModel;

namespace NodeControl
{
    [Serializable]
    [Flags]
    public enum Ports : ulong
    {
        None = 0x0,

        [Description("Port 0")]
        Port0 = 0x1,

        [Description("Port 1")]
        Port1 = 0x2,

        [Description("Port 2")]
        Port2 = 0x4,

        [Description("Port 3")]
        Port3 = 0x8,

        [Description("Port 4")]
        Port4 = 0x10,

        [Description("Port 5")]
        Port5 = 0x20,

        [Description("Port 6")]
        Port6 = 0x40,

        [Description("Port 7")]
        Port7 = 0x80,
        All = Port0 | Port1 | Port2 | Port3 | Port4 | Port5 | Port6 | Port7,
    }
}
