using System;

namespace FMOD
{
    public class AudioTrackStatusEventArgs : EventArgs
    {
        public AudioTrackStatusEventArgs(string message)
        {
            Message = message;
        }

        public string Message { get; private set; }
    }
}
