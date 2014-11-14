using BinarySerialization;

namespace XBee2
{
    public abstract class CommandResponseFrameContent : FrameContent
    {
        [SerializeAs(Order = int.MinValue)]
        public byte FrameId { get; set; }
    }
}
