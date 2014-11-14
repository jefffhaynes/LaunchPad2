using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SC18IM700.Commands
{
    public class PortConf1RegisterWrite : RegisterWrite
    {
        public PortConf1RegisterWrite()
            : base(Register.PortConf1)
        {
            Port0Config = GpioPortConfig.PushPullOutput;
            Port1Config = GpioPortConfig.PushPullOutput;
            Port2Config = GpioPortConfig.PushPullOutput;
            Port3Config = GpioPortConfig.PushPullOutput;
        }

        public GpioPortConfig Port0Config { get; set; }
        public GpioPortConfig Port1Config { get; set; }
        public GpioPortConfig Port2Config { get; set; }
        public GpioPortConfig Port3Config { get; set; }

        public override byte[] GetPacket()
        {
            byte data = 0;

            data = (byte)(data | ((byte)Port0Config));
            data = (byte)(data | ((byte)Port1Config << 2));
            data = (byte)(data | ((byte)Port2Config << 4));
            data = (byte)(data | ((byte)Port3Config << 6));

            Data = new byte[1];
            Data[0] = data;

            return base.GetPacket();
        }
    }
}
