using System;

namespace NodeControl
{
    public class StatusEventArgs : EventArgs
    {
        public StatusEventArgs(StatusCommand statusCommand)
        {
            StatusCommand = statusCommand;
        }

        public StatusCommand StatusCommand { get; private set; }
    }
}
