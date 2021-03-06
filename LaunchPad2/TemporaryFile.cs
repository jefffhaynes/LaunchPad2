﻿using System;
using System.IO;

namespace LaunchPad2
{
    public sealed class TemporaryFile : IDisposable
    {
        private string _path;

        public TemporaryFile() : this(System.IO.Path.GetTempFileName())
        {
        }

        public TemporaryFile(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            _path = path;
        }

        public string Path
        {
            get
            {
                if (_path == null) throw new ObjectDisposedException(GetType().Name);
                return _path;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        ~TemporaryFile()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                GC.SuppressFinalize(this);
            }
            if (_path != null)
            {
                try
                {
                    File.Delete(_path);
                }
                catch
                {
                } // best effort
                _path = null;
            }
        }
    }
}