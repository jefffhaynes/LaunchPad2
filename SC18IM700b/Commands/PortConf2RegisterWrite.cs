namespace SC18IM700.Commands
{
    public class PortConf2RegisterWrite : RegisterWrite
    {
        public PortConf2RegisterWrite()
            : base(Register.PortConf2)
        {
            Port4Config = GpioPortConfig.PushPullOutput;
            Port5Config = GpioPortConfig.PushPullOutput;
            Port6Config = GpioPortConfig.PushPullOutput;
            Port7Config = GpioPortConfig.PushPullOutput;
        }

        public GpioPortConfig Port4Config { get; set; }
        public GpioPortConfig Port5Config { get; set; }
        public GpioPortConfig Port6Config { get; set; }
        public GpioPortConfig Port7Config { get; set; }

        public override byte[] GetPacket()
        {
            byte data = 0;

            data = (byte)(data | ((byte)Port4Config));
            data = (byte)(data | ((byte)Port5Config << 2));
            data = (byte)(data | ((byte)Port6Config << 4));
            data = (byte)(data | ((byte)Port7Config << 6));

            Data = new byte[1];
            Data[0] = data;

            return base.GetPacket();
        }
    }
}
