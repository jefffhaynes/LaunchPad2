using System;

namespace XBee2
{
    public class XBeeException : Exception
    {
        public XBeeException()
        {
        }

        public XBeeException(string message) : base(message)
        {
        }

        public XBeeException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
