using System;
using System.Text;
using XBee.Utils;

namespace XBee.Frames
{
    public class ATCommandResponse : XBeeFrame
    {
        private const byte MinSignalLoss = 0x17;
        private const byte MaxSignalLoss = 0x64;
        private const byte SignalLossRange = MaxSignalLoss - MinSignalLoss;
        private const byte SignalLossBandSize = SignalLossRange/3;
        private const byte SignalLossHighThreshold = MaxSignalLoss - SignalLossBandSize;
        private const byte SignalLossLowThreshold = MinSignalLoss + SignalLossBandSize;

        private readonly PacketParser _parser;

        public ATCommandResponse(PacketParser parser)
        {
            _parser = parser;
            CommandId = XBeeAPICommandId.AT_COMMAND_RESPONSE;
        }

        public ATCommandResponse()
        {
            CommandId = XBeeAPICommandId.AT_COMMAND_RESPONSE;
        }

        public AT Command { get; private set; }
        public ATValue Value { get; private set; }
        public byte CommandStatus { get; private set; }

        public XBeeNodeInfo NodeInfo { get; private set; }

        public override byte[] ToByteArray()
        {
            return new byte[] {};
        }

        public override void Parse()
        {
            FrameId = (byte) _parser.ReadByte();
            Command = _parser.ReadATCommand();
            CommandStatus = (byte) _parser.ReadByte();

            if (Command == AT.NodeDiscover)
                ParseNetworkDiscovery();

            ATValueType type = ((ATAttribute) Command.GetAttr()).ValueType;

            if ((type != ATValueType.None) && _parser.HasMoreData())
            {
                switch (type)
                {
                    case ATValueType.Number:
                        byte[] vData = _parser.ReadData();
                        Value = new ATLongValue().FromByteArray(vData);
                        break;
                    case ATValueType.HexString:
                        byte[] hexData = _parser.ReadData();
                        Value = new ATStringValue(ByteUtils.ToBase16(hexData));
                        break;
                    case ATValueType.String:
                        byte[] str = _parser.ReadData();
                        Value = new ATStringValue(Encoding.UTF8.GetString(str));
                        break;
                }
            }
        }

        private void ParseNetworkDiscovery()
        {
            var source = new XBeeNode {Address16 = _parser.ReadAddress16(), Address64 = _parser.ReadAddress64()};
            int signalLossValue = _parser.ReadByte();
            string nodeIdentifier = _parser.ReadString();

            XBeeNodeSignalStrength signalStrength;
            if (signalLossValue > SignalLossHighThreshold)
                signalStrength = XBeeNodeSignalStrength.Low;
            else if (signalLossValue < SignalLossLowThreshold)
                signalStrength = XBeeNodeSignalStrength.High;
            else signalStrength = XBeeNodeSignalStrength.Medium;

            NodeInfo = new XBeeNodeInfo(source, nodeIdentifier, signalStrength);

            Console.WriteLine("source {0}, id {1} @ {2}", source.Address64, nodeIdentifier, signalLossValue);
        }
    }
}