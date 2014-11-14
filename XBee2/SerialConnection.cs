﻿using System;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using XBee2.Frames.AtCommands;

namespace XBee2
{
    internal class SerialConnection : IDisposable
    {
        private readonly SerialPort _serialPort;
        private CancellationTokenSource _readCancellationTokenSource;
        private readonly FrameSerializer _frameSerializer = new FrameSerializer();

        public SerialConnection(string port, int baudRate)
        {
            _serialPort = new SerialPort(port, baudRate);
        }

        public HardwareVersion? CoordinatorHardwareVersion
        {
            get { return _frameSerializer.CoordinatorHardwareVersion; }
            set { _frameSerializer.CoordinatorHardwareVersion = value; }
        }

        public void Send(FrameContent frameContent)
        {
            var data = _frameSerializer.Serialize(new Frame(frameContent));
            _serialPort.BaseStream.Write(data, 0, data.Length);
        }

        public event EventHandler<FrameReceivedEventArgs> FrameReceived;

        public void Open()
        {
            _serialPort.Open();

            _readCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _readCancellationTokenSource.Token;

            Task.Run(() =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var frame = _frameSerializer.Deserialize(_serialPort.BaseStream);

                        if (FrameReceived != null)
                            FrameReceived(this, new FrameReceivedEventArgs(frame.Payload.Content));
                    }
                    catch (IOException)
                    {
                        if (!cancellationToken.IsCancellationRequested)
                            throw;
                    }
                    
                }
            }, cancellationToken);
        }

        public void Close()
        {
            _readCancellationTokenSource.Cancel();
            _serialPort.Close();
        }

        public void Dispose()
        {
            Close();
        }
    }
}
