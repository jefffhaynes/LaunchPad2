using XBee2.Frames.AtCommands;

namespace XBee2
{
    public class FrameContext
    {
        public FrameContext(HardwareVersion? coordinatorHardwareVersion)
        {
            CoordinatorHardwareVersion = coordinatorHardwareVersion;
        }

        public HardwareVersion? CoordinatorHardwareVersion { get; private set; }
    }
}
