namespace SC18IM700
{   
    public abstract class CommandBase
    {
        protected CommandBase(byte command)
        {
            Command = command;
        }

        protected CommandBase(char command)
        {
            Command = (byte)command;
        }

        public byte Command { get; set; }

        public abstract byte[] GetPacket();

        //private const char I2CStart = 'S';
        //private const char I2CStop = 'P';
        //private const char RegisterRead = 'R';
        //private const char RegisterWrite = 'W';
        //private const char GpioRead = 'I';
        //private const char PowerDown = 'Z';

        public const byte EOL = (byte)'P';
    }
}
