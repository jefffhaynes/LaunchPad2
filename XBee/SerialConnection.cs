using System;
using System.IO;
using System.IO.Ports;
using XBee.Utils;

namespace XBee
{
    public class SerialConnection : IXBeeConnection, IDisposable
    {
        private readonly SerialPort _serialPort;
        private IPacketReader _reader;

        public SerialConnection(string port, int baudRate)
        {
            _serialPort = new SerialPort(port, baudRate);
            _serialPort.DataReceived += ReceiveData;
        }

        private void ReceiveData(object sender, SerialDataReceivedEventArgs e)
        {
            var length = _serialPort.BytesToRead;
            var buffer = new byte[length];

            _serialPort.Read(buffer, 0, length);

            _reader.ReceiveData(buffer);
        }

        public void Write(byte[] data)
        {
            _serialPort.Write(data, 0, data.Length);
        }

        public Stream GetStream()
        {
            return _serialPort.BaseStream;
        }

        public void Open()
        {
            _serialPort.Open();
        }

        public void Close()
        {
            _serialPort.Close();
        }

        public void SetPacketReader(IPacketReader reader)
        {
            _reader = reader;
        }

        public void Dispose()
        {
            if(_serialPort != null)
                _serialPort.Dispose();
        }
    }
}
