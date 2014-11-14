using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;

namespace FMOD
{
    public class AudioTrack : IDisposable, INotifyPropertyChanged
    {
        private const int UpdateIntervalMs = 20;
        private const int FftWindowSize = 1024;
        private const int SubbandCount = 128;
        private const int DefaultEnergySubbandWindowSize = 42;
        private static readonly LomontFft Fft = new LomontFft();
        private readonly string _file;
        private readonly Timer _positionReportingTimer;
        private readonly ReaderWriterLockSlim _samplesCacheLock = new ReaderWriterLockSlim();
        private readonly ReaderWriterLockSlim _spectralSamplesCacheLock = new ReaderWriterLockSlim();
        private readonly ReaderWriterLockSlim _energySubbandCacheLock = new ReaderWriterLockSlim();
        private CHANNEL_CALLBACK _channelCallback;
        private TemporaryFile _energySubbandCacheTempFile;
        private TimeSpan _length;
        private int _numChannels;
        private Channel _playbackChannel;
        private Sound _playbackSound;
        private TemporaryFile _sampleCacheTempFile;
        private float _sampleRate;
        private int _soundBits;
        private SOUND_FORMAT _soundFormat = SOUND_FORMAT.NONE;
        private SOUND_TYPE _soundType = SOUND_TYPE.UNKNOWN;
        private TemporaryFile _spectralCacheTempFile;
        private FmodSystem _system;
        private uint _totalSamples;
        private Channel _viewChannel;
        private Sound _viewSound;

        public AudioTrack(string file)
        {
            _file = file;

            _positionReportingTimer = new Timer(UpdateIntervalMs) {Enabled = false, AutoReset = true};
            _positionReportingTimer.Elapsed += ReportingTimerElapsed;

            EnergySubbandWindowSize = DefaultEnergySubbandWindowSize;

            Load();
        }


        public bool IsPaused
        {
            get
            {
                if (_playbackChannel == null)
                    return false;

                bool paused = false;
                RESULT result = _playbackChannel.getPaused(ref paused);
                ThrowOnBadResult(result);
                return paused;
            }

            set
            {
                if (value != IsPaused)
                {
                    RESULT result = _playbackChannel.setPaused(value);
                    ThrowOnBadResult(result);
                    OnPropertyChanged("IsPaused");
                    _positionReportingTimer.Enabled = !value;
                }
            }
        }

        public TimeSpan Position
        {
            get
            {
                uint ms = 0;
                RESULT result = _playbackChannel.getPosition(ref ms, TIMEUNIT.MS);
                if (result != RESULT.ERR_INVALID_HANDLE)
                    ThrowOnBadResult(result);
                return TimeSpan.FromMilliseconds(ms);
            }

            set
            {
                if (value > Length)
                    throw new ArgumentOutOfRangeException("value", "Must be less than the sound length");
                if (value < TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException("value", "Cannot be negative");

                RESULT result = _playbackChannel.setPosition((uint) value.TotalMilliseconds, TIMEUNIT.MS);
                ThrowOnBadResult(result);
                OnPropertyChanged("Position");
                OnPropertyChanged("SamplePosition");

                if (PositionChanged != null)
                    PositionChanged(this, EventArgs.Empty);
            }
        }

        public uint SamplePosition
        {
            get
            {
                uint pcm = 0;
                RESULT result = _playbackChannel.getPosition(ref pcm, TIMEUNIT.PCM);

                if (result != RESULT.ERR_INVALID_HANDLE)
                    ThrowOnBadResult(result);

                return pcm;
            }

            set
            {
                RESULT result = _playbackChannel.setPosition(
                    value > TotalSamples ? TotalSamples - 1 : value,
                    TIMEUNIT.PCM);

                ThrowOnBadResult(result);
                OnPropertyChanged("Position");
                OnPropertyChanged("SamplePosition");
            }
        }

        public TimeSpan Length
        {
            get { return _length; }
        }

        public uint TotalSamples
        {
            get { return _totalSamples; }
        }

        public int Bits
        {
            get { return _soundBits; }
        }

        public float Volume
        {
            get
            {
                float vol = 0;
                RESULT result = _playbackChannel.getVolume(ref vol);
                ThrowOnBadResult(result);
                return vol;
            }

            set
            {
                if (value > 1.0f || value < 0.0f)
                    throw new ArgumentOutOfRangeException("value", "Must be between 0.0 and 1.0");

                if (Math.Abs(value - Volume) > float.Epsilon)
                {
                    RESULT result = _playbackChannel.setVolume(value);
                    ThrowOnBadResult(result);
                    OnPropertyChanged("Volume");
                }
            }
        }

        public int ChannelCount
        {
            get { return _numChannels; }
        }

        public float SampleRate
        {
            get { return _sampleRate; }
            set { _sampleRate = value; }
        }

        public IEnumerable<StereoSample> Samples
        {
            get
            {
                if (_sampleCacheTempFile == null)
                    CacheSamples();

                _samplesCacheLock.EnterReadLock();

                try
                {
                    using (var stream = new FileStream(_sampleCacheTempFile.Path, FileMode.Open, FileAccess.Read))
                    using (var reader = new BinaryReader(stream))
                    {
                        var sampleCount = stream.Length/(sizeof (float)*2);
                        for (int i = 0; i < sampleCount; i++)
                        {
                            float left = reader.ReadSingle();
                            float right = reader.ReadSingle();
                            yield return new StereoSample(left, right);
                        }
                    }
                }
                finally
                {
                    _samplesCacheLock.ExitReadLock();
                }
            }
        }

        public IEnumerable<double[]> SpectralSamples
        {
            get
            {
                if (_spectralCacheTempFile == null)
                    CacheSpectralSamples();

                _spectralSamplesCacheLock.EnterReadLock();

                try
                {
                    using (var stream = new FileStream(_spectralCacheTempFile.Path, FileMode.Open, FileAccess.Read))
                    using (var reader = new BinaryReader(stream))
                    {
                        var frameCount = stream.Length/(FftWindowSize*sizeof (double));
                        for (int i = 0; i < frameCount; i++)
                        {
                            yield return Enumerable.Range(0, FftWindowSize)
                                .Select(bands => reader.ReadDouble()).ToArray();
                        }
                    }
                }
                finally
                {
                    _spectralSamplesCacheLock.ExitReadLock();
                }
            }
        }

        public int EnergySubbandWindowSize { get; private set; }

        public IEnumerable<double[]> EnergySubbands
        {
            get
            {
                if (_energySubbandCacheTempFile == null)
                    CacheEnergySubbands();

                _energySubbandCacheLock.EnterReadLock();

                try
                {
                    using (var stream = new FileStream(_energySubbandCacheTempFile.Path, FileMode.Open, FileAccess.Read)
                        )
                    using (var reader = new BinaryReader(stream))
                    {
                        int length = (int) stream.Length/(SubbandCount*sizeof (double));
                        for (int i = 0; i < SubbandCount; i++)
                        {
                            var data = Enumerable.Range(0, length)
                                .Select(bands => reader.ReadDouble()).ToArray();

                            Array.Resize(ref data, data.Length + EnergySubbandWindowSize);

                            yield return data;
                        }
                    }
                }
                finally
                {
                    _energySubbandCacheLock.ExitReadLock();
                }
            }
        }

        public event EventHandler PositionChanged;

        private void CacheSamples()
        {
            _samplesCacheLock.EnterWriteLock();

            if (_sampleCacheTempFile != null)
                return;

            try
            {
                _sampleCacheTempFile = new TemporaryFile();
                using (var stream = new FileStream(_sampleCacheTempFile.Path, FileMode.Create, FileAccess.Write))
                using (var writer = new BinaryWriter(stream))
                {
                    foreach (StereoSample sample in ReadSamples())
                    {
                        writer.Write(sample.Left);
                        writer.Write(sample.Right);
                    }
                }
            }
            finally
            {
                _samplesCacheLock.ExitWriteLock();
            }
        }

        private IEnumerable<StereoSample> ReadSamples()
        {
            const int blockLength = 4096;
            const float singleMaxValue = 0.01f;
            const float int16MaxValue = Int16.MaxValue;

            int sampleSize = Bits/8*ChannelCount;

            uint read = 0;
            var block = new byte[blockLength];

            _viewSound.seekData(0);

            IntPtr blockPtr = Marshal.AllocHGlobal(blockLength);
            try
            {
                RESULT result;
                do
                {
                    result = _viewSound.readData(blockPtr, blockLength, ref read);

                    if (result != RESULT.ERR_FILE_EOF)
                        ThrowOnBadResult(result);

                    Marshal.Copy(blockPtr, block, 0, (int) read);

                    switch (ChannelCount)
                    {
                        case 1:
                            switch (Bits)
                            {
                                case 16:
                                    for (int i = 0; i < read; i += sampleSize)
                                        yield return
                                            new StereoSample(BitConverter.ToInt16(block, i)/int16MaxValue);
                                    break;
                                case 32:
                                    for (int i = 0; i < read; i += sampleSize)
                                        yield return
                                            new StereoSample(BitConverter.ToSingle(block, i)/singleMaxValue);
                                    break;
                            }
                            break;
                        case 2:
                            switch (Bits)
                            {
                                case 16:
                                    for (int i = 0; i < read; i += sampleSize)
                                        yield return
                                            new StereoSample(
                                                BitConverter.ToInt16(block, i)/int16MaxValue,
                                                BitConverter.ToInt16(block, i + 2)/int16MaxValue);
                                    break;
                                case 32:
                                    for (int i = 0; i < read; i += sampleSize)
                                        yield return new StereoSample(
                                            BitConverter.ToSingle(block, i)/singleMaxValue,
                                            BitConverter.ToSingle(block, i + 4)/singleMaxValue);
                                    break;
                            }
                            break;
                    }
                } while (result != RESULT.ERR_FILE_EOF);
            }
            finally
            {
                Marshal.FreeHGlobal(blockPtr);
            }
        }

        private void CacheSpectralSamples()
        {
            _spectralSamplesCacheLock.EnterWriteLock();

            try
            {
                _spectralCacheTempFile = new TemporaryFile();

                using (var stream = new FileStream(_spectralCacheTempFile.Path, FileMode.Create, FileAccess.Write))
                {
                    using (var writer = new BinaryWriter(stream))
                    {
                        foreach (var sample in GetFrames(Samples))
                        {
                            foreach (double value in sample)
                                writer.Write(value);
                        }
                    }
                }
            }
            finally
            {
                _spectralSamplesCacheLock.ExitWriteLock();
            }
        }

        private void CacheEnergySubbands()
        {
            _energySubbandCacheLock.EnterWriteLock();

            try
            {
                _energySubbandCacheTempFile = new TemporaryFile();

                using (var stream = new FileStream(_energySubbandCacheTempFile.Path, FileMode.Create, FileAccess.Write))
                {
                    using (var writer = new BinaryWriter(stream))
                    {
                        IEnumerable<IEnumerable<double>> energySubbands = ReadEnergySubbands();
                        foreach (var sample in energySubbands)
                        {
                            foreach (double value in sample)
                                writer.Write(value);
                        }
                    }
                }
            }
            finally
            {
                _energySubbandCacheLock.ExitWriteLock();
            }
        }

        private IEnumerable<IEnumerable<double>> ReadEnergySubbands()
        {
            IEnumerable<IEnumerable<double>> frameSubbandEnergies = SpectralSamples
                .Select(GetSubbandEnergies);

            IEnumerable<IEnumerable<IEnumerable<double>>> subbandEnergyFrameWindows =
                frameSubbandEnergies.ZipMany(
                    frameSubbandEnergy => frameSubbandEnergy.Window(EnergySubbandWindowSize));

            IEnumerable<IEnumerable<double>> locallyComparedSubbandEnergy = subbandEnergyFrameWindows.Select(
                subbandEnergyWindowFrames =>
                    subbandEnergyWindowFrames.Select(subbandEnergyWindowFrame =>
                    {
                        double[] subbandEnergyWindowFrameData = subbandEnergyWindowFrame.ToArray();
                        double average = subbandEnergyWindowFrameData.Average();
                        double first = subbandEnergyWindowFrameData.First();
                        return first - average;
                    }));

            return locallyComparedSubbandEnergy;
        }

        private static IEnumerable<double[]> GetFrames(IEnumerable<StereoSample> samples)
        {
            IEnumerable<IEnumerable<StereoSample>> frames = samples.Batch(FftWindowSize);

            double[] zeroData = Enumerable.Repeat(0.0, FftWindowSize).ToArray();

            IEnumerable<double[]> spectralFrames = frames.Select(frame =>
            {
                StereoSample[] data = frame.ToArray();
                Array.Resize(ref data, FftWindowSize);

                IEnumerable<double> averageData = data.Select(sample => (double) (sample.Left + sample.Right)/2);

                double[] complexData = averageData.Interleave(zeroData).ToArray();

                Fft.RealFFT(complexData, true);

                IEnumerable<double> realSpectralData = complexData.TakeEvery(2);
                IEnumerable<double> imaginarySpectralData = complexData.Skip(1).TakeEvery(2);

                IEnumerable<double> spectralMagData = realSpectralData.Zip(imaginarySpectralData,
                    (r, i) => Math.Sqrt(r*r + i*i));

                return spectralMagData.ToArray();
            });

            return spectralFrames;
        }

        private static IEnumerable<double> GetSubbandEnergies(double[] window)
        {
            int subbandLength = window.Length/SubbandCount;
            IEnumerable<IEnumerable<double>> subbands = window.Batch(subbandLength);
            IEnumerable<double> subbandEnergies = subbands.Select(subband => subband.Sum());

            double scalar = (double) SubbandCount/window.Length;
            return subbandEnergies.Select(subbandEnergy => subbandEnergy*scalar);
        }

        private void Load()
        {
            RESULT result = Factory.System_Create(ref _system);
            ThrowOnBadResult(result);

            result = _system.init(2, INITFLAGS.NORMAL, (IntPtr) null);
            ThrowOnBadResult(result);

            result = _system.createStream(_file, MODE._2D | MODE.HARDWARE | MODE.CREATESTREAM | MODE.ACCURATETIME,
                ref _viewSound);
            ThrowOnBadResult(result);

            result = _system.playSound(CHANNELINDEX.FREE, _viewSound, true, ref _viewChannel);
            ThrowOnBadResult(result);

            result = _system.createStream(_file, MODE._2D | MODE.HARDWARE | MODE.CREATESTREAM | MODE.ACCURATETIME,
                ref _playbackSound);

            ThrowOnBadResult(result);
            result = _system.playSound(CHANNELINDEX.FREE, _playbackSound, true, ref _playbackChannel);
            ThrowOnBadResult(result);

            /* Keep reference to channel callback or it will be collected */
            _channelCallback = ChannelCallback;
            result = _playbackChannel.setCallback(_channelCallback);
            ThrowOnBadResult(result);

            /* Store length so we don't have to get it every time. */
            uint ms = 0;
            result = _playbackSound.getLength(ref ms, TIMEUNIT.MS);
            ThrowOnBadResult(result);
            _length = TimeSpan.FromMilliseconds(ms);

            uint samples = 0;
            result = _playbackSound.getLength(ref samples, TIMEUNIT.PCM);
            ThrowOnBadResult(result);
            _totalSamples = samples;

            Analyze();
        }

        private RESULT ChannelCallback(IntPtr channelraw, CHANNEL_CALLBACKTYPE type, IntPtr commanddata1,
            IntPtr commanddata2)
        {
            switch (type)
            {
                case CHANNEL_CALLBACKTYPE.END:
                    _positionReportingTimer.Enabled = false;
                    Load();
                    break;
            }
            return RESULT.OK;
        }

        private void Analyze()
        {
            RESULT result = _viewSound.getFormat(ref _soundType, ref _soundFormat, ref _numChannels, ref _soundBits);
            ThrowOnBadResult(result);

            float volume = 0;
            float pan = 0;
            int priority = 0;

            _viewSound.getDefaults(ref _sampleRate, ref volume, ref pan, ref priority);

            OnPropertyChanged("SampleRate");
        }

        private void ReportingTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _system.update();
            OnPropertyChanged("Position");
            OnPropertyChanged("SamplePosition");

            if (PositionChanged != null)
                PositionChanged(this, EventArgs.Empty);
        }

        public double ToSamples(TimeSpan time)
        {
            double totalSeconds = time.TotalMilliseconds/1000;
            return totalSeconds*SampleRate;
        }

        public TimeSpan ToTime(double samples)
        {
            return TimeSpan.FromMilliseconds(samples*1000/(SampleRate));
        }

        private static void ThrowOnBadResult(RESULT result)
        {
            if (result != RESULT.OK)
            {
                throw new FmodException(Error.String(result));
            }
        }

        protected void OnPropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }

        #region IDisposable Members

        public void Dispose()
        {
            RESULT result;

            if (_playbackSound != null)
            {
                result = _playbackSound.release();
                ThrowOnBadResult(result);
            }

            if (_system != null)
            {
                result = _system.close();
                ThrowOnBadResult(result);
                result = _system.release();
                ThrowOnBadResult(result);
            }

            if (_sampleCacheTempFile != null)
                _sampleCacheTempFile.Dispose();

            if (_spectralCacheTempFile != null)
                _spectralCacheTempFile.Dispose();

            if (_energySubbandCacheTempFile != null)
                _energySubbandCacheTempFile.Dispose();
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}