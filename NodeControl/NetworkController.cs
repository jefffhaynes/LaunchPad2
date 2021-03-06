﻿using System;
using System.IO.Ports;
using System.Threading;
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
        private static bool _portChange = true;
        private static readonly SemaphoreSlim InitializeSemaphore = new SemaphoreSlim(1);

        public static event EventHandler InitializingController;

        public static event EventHandler DiscoveringNetwork;

        static NetworkController()
        {
            SerialPortService.PortsChanged += (sender, args) => _portChange = true;
        }

        public static async Task DiscoverNetworkAsync()
        {
            InitializingController?.Invoke(null, EventArgs.Empty);

            if(!await Initialize())
                throw new InvalidOperationException("No controller found.");

            DiscoveringNetwork?.Invoke(null, EventArgs.Empty);

            await _xBee.DiscoverNetworkAsync(TimeSpan.FromSeconds(10));
        }

        public static async Task<bool> Initialize()
        {
            if (_isInitialized)
                return true;
            
            // Don't bother checking if nothing has changed
            if (!_portChange)
                return false;

            await InitializeSemaphore.WaitAsync();

            try
            {
                if (_isInitialized)
                    return true;

                _xBee = await XBeeController.FindAndOpenAsync(SerialPort.GetPortNames(), 9600);

                _portChange = false;

                if (_xBee == null)
                    return false;

#if TRACE
                _xBee.FrameMemberDeserializing += XBeeOnFrameMemberDeserializing;
                _xBee.FrameMemberDeserialized += XBeeOnFrameMemberDeserialized;
#endif

                _xBee.NodeDiscovered += XBeeOnNodeDiscovered;
                _isInitialized = true;
            }
            finally
            {
                InitializeSemaphore.Release();
            }

            return true;
        }

#if TRACE
        private static void XBeeOnFrameMemberDeserializing(object sender, MemberSerializingEventArgs e)
        {
            var message = $"D-Start: {e.MemberName}";
            System.Diagnostics.Trace.WriteLine(message, "XBee");
        }

        private static void XBeeOnFrameMemberDeserialized(object sender, MemberSerializedEventArgs e)
        {
            var value = e.Value ?? "null";
            var message = $"D-End: {e.MemberName} ({value})";
            System.Diagnostics.Trace.WriteLine(message, "XBee");
        }
#endif

        public static async void SetActivePorts(NodeAddress address, Ports ports)
        {
            if (!await Initialize())
                return;

            XBeeNode node = await _xBee.GetNodeAsync(address);

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
            if (!await Initialize())
                throw new InvalidOperationException("No controller found.");

            XBeeNode node = await _xBee.GetNodeAsync(address);
            await node.SetInputOutputConfigurationAsync(ArmingPort, InputOutputConfiguration.DigitalHigh);
        }

        public static async Task Disarm(NodeAddress address)
        {
            if (!await Initialize())
                throw new InvalidOperationException("No controller found.");

            XBeeNode node = await _xBee.GetNodeAsync(address);
            await node.SetInputOutputConfigurationAsync(ArmingPort, InputOutputConfiguration.DigitalLow);
        }

        public static async Task SetNodeName(NodeAddress address, string name)
        {
            if(!await Initialize())
                throw new InvalidOperationException("No controller found.");

            XBeeNode node = await _xBee.GetNodeAsync(address);
            await node.SetNodeIdentifierAsync(name);
            await node.WriteChangesAsync();
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

            NodeDiscovered?.Invoke(null, e);
            
            Initialize(node);
        }
    }
}