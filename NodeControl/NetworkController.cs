using System;
using System.IO.Ports;
using System.Threading.Tasks;
using SC18IM700;
using SC18IM700.Commands;
using XBee;
using XBee.Frames.AtCommands;

namespace NodeControl
{
    public static class NetworkController
    {
        private static XBeeController _xBee = new XBeeController();
        private static bool _isInitialized;

        public static async Task DiscoverNetworkAsync()
        {
            await Initialize();
            await _xBee.DiscoverNetwork();
        }

        static public event EventHandler InitializingController;

        public static event EventHandler DiscoveringNetwork;

        public static async void SetActivePorts(NodeAddress address, Ports ports)
        {
            var node = await _xBee.GetRemoteAsync(address);

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
            
            await node.TransmitDataAsync(gpioWrite.GetPacket());
        }

        public static async Task SetNodeName(NodeAddress address, string name)
        {
            var node = await _xBee.GetRemoteAsync(address);
            await node.SetNodeIdentifier(name);
            await node.WriteChanges();
        }

        private static async void Initialize(XBeeNode node)
        {
            try
            {
                await Task.Delay(2000);

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

        private async static Task Initialize()
        {
            if (!_isInitialized)
            {
                _xBee = await XBeeController.FindAndOpen(SerialPort.GetPortNames(), 9600);

                if (_xBee == null)
                    throw new InvalidOperationException("No XBee found.");
                
                await _xBee.Local.Reset();
                await Task.Delay(5000);
                _xBee.NodeDiscovered += XBeeOnNodeDiscovered;
                _isInitialized = true;
            }
        }

        public static event EventHandler<NodeDiscoveredEventArgs> NodeDiscovered; 

        private static void XBeeOnNodeDiscovered(object sender, NodeDiscoveredEventArgs e)
        {
            var node = e.Node;

            if(NodeDiscovered != null)
                NodeDiscovered(null, e);

            if (_xBee.HardwareVersion == HardwareVersion.XBeeSeries1 ||
                _xBee.HardwareVersion == HardwareVersion.XBeeProSeries1)
                Initialize(node);
        }
    }
}
