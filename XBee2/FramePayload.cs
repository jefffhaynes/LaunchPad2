using BinarySerialization;
using XBee2.Frames;

namespace XBee2
{
    public class FramePayload
    {
        public FramePayload()
        {
        }

        public FramePayload(FrameContent content)
        {
            Content = content;
        }

        public FrameType FrameType { get; set; }

        [Subtype("FrameType", FrameType.AtCommand, typeof(AtCommandFrameContent))]
        [Subtype("FrameType", FrameType.AtCommandResponse, typeof(AtCommandResponseFrame))]
        [Subtype("FrameType", FrameType.TxRequest, typeof(TxRequestFrame))]
        [Subtype("FrameType", FrameType.TxRequestExt, typeof(TxRequestExtFrame))]
        [Subtype("FrameType", FrameType.TxStatus, typeof(TxStatusFrame))]
        [Subtype("FrameType", FrameType.TxStatusExt, typeof(TxStatusExtFrame))]
        [Subtype("FrameType", FrameType.ModemStatus, typeof(ModemStatusFrame))]
        [Subtype("FrameType", FrameType.RxIndicatorExt, typeof(RxIndicatorExtFrame))]
        [Subtype("FrameType", FrameType.RxIndicatorExplicitExt, typeof(RxIndicatorExplicitExtFrame))]
        public FrameContent Content { get; set; }
    }
}
