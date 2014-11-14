using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FMOD
{
    [Serializable]
    public class FmodException : Exception
    {
        public FmodException() { }
        public FmodException(string message) : base(message) { }
        public FmodException(string message, Exception inner) : base(message, inner) { }
        protected FmodException(
          global::System.Runtime.Serialization.SerializationInfo info,
          global::System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
