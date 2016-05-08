using System;
using System.IO.Ports;
using System.Linq;
using System.Management;

namespace NodeControl
{
    public static class SerialPortService
    {
        private static string[] _serialPorts;

        private static ManagementEventWatcher _arrival;
        private static ManagementEventWatcher _removal;

        static SerialPortService()
        {
            _serialPorts = GetAvailableSerialPorts();
            MonitorDeviceChanges();
        }

        public static void CleanUp()
        {
            _arrival.Stop();
            _removal.Stop();
        }

        public static event EventHandler<PortsChangedArgs> PortsChanged;

        private static void MonitorDeviceChanges()
        {
            try
            {
                var deviceArrivalQuery = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2");
                var deviceRemovalQuery = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 3");

                _arrival = new ManagementEventWatcher(deviceArrivalQuery);
                _removal = new ManagementEventWatcher(deviceRemovalQuery);

                _arrival.EventArrived += (o, args) => OnPortsChanged(EventType.Insertion);
                _removal.EventArrived += (sender, eventArgs) => OnPortsChanged(EventType.Removal);

                // Start listening for events
                _arrival.Start();
                _removal.Start();
            }
            catch (ManagementException)
            {

            }
        }

        private static void OnPortsChanged(EventType eventType)
        {
            lock (_serialPorts)
            {
                var availableSerialPorts = GetAvailableSerialPorts();
                if (!_serialPorts.SequenceEqual(availableSerialPorts))
                {
                    _serialPorts = availableSerialPorts;
                    PortsChanged?.Invoke(null, new PortsChangedArgs(eventType, _serialPorts));
                }
            }
        }

        public static string[] GetAvailableSerialPorts()
        {
            return SerialPort.GetPortNames();
        }
    }
}
