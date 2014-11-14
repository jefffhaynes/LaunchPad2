namespace XBee2.Frames.AtCommands
{
    public class HardwareVersionResponseData : AtCommandResponseFrameData
    {
        public HardwareVersion HardwareVersion { get; set; }

        public byte HardwareRevision { get; set; }
    }
}
