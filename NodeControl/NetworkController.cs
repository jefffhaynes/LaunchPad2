using System;
using System.Collections.Generic;
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
        private static readonly XBeeController XBee = new XBeeController();
        private static readonly object InitializingLock = new object();
        private static bool _isInitialized;

        private static IEnumerable<string> SerialPortNames
        {
            get { return SerialPort.GetPortNames(); }
        }

        public static async Task DiscoverNetworkAsync()
        {
            await Initialize();
            await XBee.DiscoverNetwork();
        }

        public static async void SetActivePorts(NodeAddress address, Ports ports)
        {
            var node = await XBee.GetRemote(address);

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
            var node = await XBee.GetRemote(address);
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
            catch (TimeoutException)
            {
            }
        }

        private async static Task Initialize()
        {
            if (!_isInitialized)
            {
                await XBee.OpenAsync("COM5", 9600);
                //await XBee.OpenAsync("COM4", 115200);
                XBee.NodeDiscovered += XBeeOnNodeDiscovered;
                _isInitialized = true;
            }
        }

        public static event EventHandler<NodeDiscoveredEventArgs> NodeDiscovered; 

        private static void XBeeOnNodeDiscovered(object sender, NodeDiscoveredEventArgs e)
        {
            //ConnectionQuality connectionQuality;
            //switch (e.SignalStrength)
            //{
            //    case SignalStrength.Low:
            //        connectionQuality = ConnectionQuality.Low;
            //        break;
            //    case SignalStrength.Medium:
            //        connectionQuality = ConnectionQuality.Medium;
            //        break;
            //    case SignalStrength.High:
            //        connectionQuality = ConnectionQuality.High;
            //        break;
            //    default:
            //        throw new InvalidOperationException("Unknown signal strength.");
            //}

            var node = e.Node;

            if(NodeDiscovered != null)
                NodeDiscovered(null, e);

            if (XBee.HardwareVersion == HardwareVersion.XBeeSeries1 ||
                XBee.HardwareVersion == HardwareVersion.XBeeProSeries1)
                Initialize(node);
        }
    }
}
