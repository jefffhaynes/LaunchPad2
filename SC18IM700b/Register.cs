namespace SC18IM700
{
    public enum Register : byte
    {
        BRG0 = 0x0,
        BRG1 = 0x1,
        PortConf1 = 0x2,
        PortConf2 = 0x3,
        IOState = 0x4,
        I2CAdr = 0x6,
        I2CClkL = 0x7,
        I2CClkH = 0x8,
        I2CTO = 0x9,
        I2CStat = 0x0A
    }
}
