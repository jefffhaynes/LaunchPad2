using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using XBee.Frames;

namespace XBee
{
    public enum ApiTypeValue : byte
    {
        Disabled = 0x00,
        Enabled = 0x01,
        EnabledWithEscape = 0x02,
        Unknown = 0xFF
    }

    public class XBee
    {
        private IXBeeConnection _connection;

        private byte _frameId = byte.MinValue;

        private IPacketReader _reader;
        private ApiTypeValue _apiType;

        private static readonly ConcurrentDictionary<byte, TaskCompletionSource<XBeeFrame>> ExecuteTaskCompletionSources =
            new ConcurrentDictionary<byte, TaskCompletionSource<XBeeFrame>>();

        private static readonly ConcurrentDictionary<byte, Action<XBeeFrame>> ExecuteCallbacks =
            new ConcurrentDictionary<byte, Action<XBeeFrame>>();

        private static readonly SemaphoreSlim OperationLock = new SemaphoreSlim(1);

        public ApiTypeValue ApiType
        {
            get { return _apiType; }
            set
            {
                _apiType = value;
                _reader = PacketReaderFactory.GetReader(_apiType);
                _reader.FrameReceived += FrameReceivedEvent;
            }
        }

        public void SetConnection(IXBeeConnection connection)
        {
            _connection = connection;
            _connection.Open();
            _connection.SetPacketReader(_reader);
        }

        public void Execute(XBeeFrame frame)
        {
            var packet = new XBeePacket(frame);
            packet.Assemble();
            _connection.Write(packet.Data);
        }

        public Task<XBeeFrame> ExecuteQueryAsync(XBeeFrame frame)
        {
            return ExecuteQueryAsync(frame, TimeSpan.FromSeconds(1));
        }

        public async Task<XBeeFrame> ExecuteQueryAsync(XBeeFrame frame, TimeSpan timeout)
        {
            await OperationLock.WaitAsync();

            try
            {
                unchecked
                {
                    frame.FrameId = ++_frameId;
                }

                var delayCancellationTokenSource = new CancellationTokenSource();
                var delayTask = Task.Delay(timeout, delayCancellationTokenSource.Token);

                var taskCompletionSource = ExecuteTaskCompletionSources.AddOrUpdate(frame.FrameId,
                    b => new TaskCompletionSource<XBeeFrame>(),
                    (b, source) => new TaskCompletionSource<XBeeFrame>());

                Execute(frame);

                if (await Task.WhenAny(taskCompletionSource.Task, delayTask) == taskCompletionSource.Task)
                {
                    delayCancellationTokenSource.Cancel();
                    return await taskCompletionSource.Task;
                }

                throw new TimeoutException();
            }
            finally
            {
                OperationLock.Release();
            }
        }

        public async void ExecuteMultiQueryAsync<TCallbackFrame>(XBeeFrame frame, Action<TCallbackFrame> callback, TimeSpan timeout) where TCallbackFrame : XBeeFrame
        {
            await OperationLock.WaitAsync();

            try
            {
                unchecked
                {
                    frame.FrameId = ++_frameId;
                }

                /* Make sure callback is in this context */
                var context = SynchronizationContext.Current;
                var callbackProxy = new Action<XBeeFrame>(callbackFrame =>
                    context.Post(state => callback((TCallbackFrame) callbackFrame), null));

                ExecuteCallbacks.AddOrUpdate(frame.FrameId, b => callbackProxy, (b, source) => callbackProxy);

                Execute(frame);

                await Task.Delay(timeout);

                Action<XBeeFrame> action;
                ExecuteCallbacks.TryRemove(frame.FrameId, out action);
            }
            finally
            {
                OperationLock.Release();
            }
        }

        public async Task<XBeeFrame> TransmitDataAsync(XBeeAddress64 address, byte[] data)
        {
            var transmitRequest = new TransmitData64(address, data);
            return await ExecuteQueryAsync(transmitRequest);
        }

        public void TransmitData(XBeeAddress64 address, byte[] data)
        {
            var transmitRequest = new TransmitData64(address, data, true);

            unchecked
            {
                transmitRequest.FrameId = ++_frameId;
            }

            Execute(transmitRequest);
        }

        public void FrameReceivedEvent(object sender, FrameReceivedArgs args)
        {
            var frameId = args.Response.FrameId;

            TaskCompletionSource<XBeeFrame> taskCompletionSource;
            if (ExecuteTaskCompletionSources.TryRemove(frameId, out taskCompletionSource))
            {
                taskCompletionSource.SetResult(args.Response);
            }
            else
            {
                Action<XBeeFrame> callback;
                if (ExecuteCallbacks.TryGetValue(frameId, out callback))
                {
                    callback(args.Response);
                }
            }
        }
    }
}
