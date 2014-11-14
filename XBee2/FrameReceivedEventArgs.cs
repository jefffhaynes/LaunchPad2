using System;

namespace XBee2
{
    public class FrameReceivedEventArgs : EventArgs
    {
        public FrameReceivedEventArgs(FrameContent frameContent)
        {
            FrameContent = frameContent;
        }

        public FrameContent FrameContent { get; private set; }
    }
}
