using System;
using System.Runtime.Serialization;

namespace LiveStreamingWebRTC.Exceptions
{
    [Serializable]
    internal class ClientLimitReachedException : Exception
    {
        public ClientLimitReachedException()
        {
        }

        public ClientLimitReachedException(string message) : base(message)
        {
        }

        public ClientLimitReachedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ClientLimitReachedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}