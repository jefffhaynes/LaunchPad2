using System;
using System.IO.Ports;
using System.Threading.Tasks;
using BinarySerialization;
using SC18IM700;
using SC18IM700.Commands;
using XBee;
using XBee.Frames.AtCommands;

namespace NodeControl
{
    public static class NetworkController
    {
        private const InputOutputChannel ArmingPort = InputOutputChannel.Channel7;

        private static XBeeController _xBee;
        private static bool _isInitialized;

        public static event EventHandler InitializingController;

        public static event EventHandler DiscoveringNetwork;

        public static async Task DiscoverNetworkAsync()
        {
            if (InitializingController != null)
                InitializingController(null, EventArgs.Empty);

            await Initialize();

            if (DiscoveringNetwork != null)
                DiscoveringNetwork(null, EventArgs.Empty);

            await _xBee.DiscoverNetwork(TimeSpan.FromSeconds(10));
        }

        public static async Task Initialize()
        {
            if (!_isInitialized)
            {
                _xBee = await XBeeController.FindAndOpen(SerialPort.GetPortNames(), 9600);

                if (_xBee == null)
                    throw new InvalidOperationException("No XBee found.");

#if TRACE
                _xBee.FrameMemberDeserializing += XBeeOnFrameMemberDeserializing;
                _xBee.FrameMemberDeserialized += XBeeOnFrameMemberDeserialized;
#endif

                _xBee.NodeDiscovered += XBeeOnNodeDiscovered;
                _isInitialized = true;
            }
        }

#if TRACE
        private static void XBeeOnFrameMemberDeserializing(object sender, MemberSerializingEventArgs e)
        {
            var message = string.Format("D-Start: {0}", e.MemberName);
            System.Diagnostics.Trace.WriteLine(message, "XBee");
        }

        private static void XBeeOnFrameMemberDeserialized(object sender, MemberSerializedEventArgs e)
        {
            var value = e.Value ?? "null";
            var message = string.Format("D-End: {0} ({1})", e.MemberName, value);
            System.Diagnostics.Trace.WriteLine(message, "XBee");
        }
#endif

        public static async void SetActivePorts(NodeAddress address, Ports ports)
        {
            XBeeNode node = await _xBee.GetRemoteAsync(address);

            var gpioPorts = GpioPorts.None;

            if (ports != Ports.None)
            {
                if (ports.HasFlag(Ports.Port0))
                    gpioPorts |= GpioPorts.Port0;
                if (ports.HasFlag(Ports.Port1))
                    gpioPorts |= GpioPorts.Port1;
                if (ports.HasFlag(Ports.Port2))
                    gpioPorts |= GpioPorts.Port2;
                if (ports.HasFlag(Ports.Port3))
                    gpioPorts |= GpioPorts.Port3;
                if (ports.HasFlag(Ports.Port4))
                    gpioPorts |= GpioPorts.Port4;
                if (ports.HasFlag(Ports.Port5))
                    gpioPorts |= GpioPorts.Port5;
                if (ports.HasFlag(Ports.Port6))
                    gpioPorts |= GpioPorts.Port6;
                if (ports.HasFlag(Ports.Port7))
                    gpioPorts |= GpioPorts.Port7;
            }

            var gpioWrite = new GpioWrite(gpioPorts);

            await node.TransmitDataAsync(gpioWrite.GetPacket(), false);
        }

        public static async Task Arm(NodeAddress address)
        {
            XBeeNode node = await _xBee.GetRemoteAsync(address);
            await node.SetInputOutputConfiguration(ArmingPort, InputOutputConfiguration.DigitalHigh);
        }

        public static async Task Disarm(NodeAddress address)
        {
            XBeeNode node = await _xBee.GetRemoteAsync(address);
            await node.SetInputOutputConfiguration(ArmingPort, InputOutputConfiguration.DigitalLow);
        }

        public static async Task SetNodeName(NodeAddress address, string name)
        {
            XBeeNode node = await _xBee.GetRemoteAsync(address);
            await node.SetNodeIdentifier(name);
            await node.WriteChanges();
        }

        private static async void Initialize(XBeeNode node)
        {
            try
            {
                var port1Conf = new PortConf1RegisterWrite();
                await node.TransmitDataAsync(port1Conf.GetPacket());

                var port2Conf = new PortConf2RegisterWrite();
                await node.TransmitDataAsync(port2Conf.GetPacket());

                SetActivePorts(node.Address, Ports.None);
            }
            catch (XBeeException)
            {
            }
            catch (TimeoutException)
            {
            }
        }

        public static event EventHandler<NodeDiscoveredEventArgs> NodeDiscovered;

        private static void XBeeOnNodeDiscovered(object sender, NodeDiscoveredEventArgs e)
        {
            XBeeNode node = e.Node;

            if (NodeDiscovered != null)
                NodeDiscovered(null, e);

            if (IsSeriesOne)
                Initialize(node);
        }

        private static bool IsSeriesOne
        {
            get
            {
                return _xBee.HardwareVersion == HardwareVersion.XBeeSeries1 ||
                       _xBee.HardwareVersion == HardwareVersion.XBeeProSeries1;
            }
        }
    }
}