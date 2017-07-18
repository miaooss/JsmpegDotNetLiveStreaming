using Fleck;
using System;
namespace LiveStreamingWebRTC
{
    public class ClientSession
    {
        public IWebSocketConnection Context { get; private set; }
        public string RtspChannelUrl { get; private set; }

        public ClientSession(IWebSocketConnection context, string rtspChannelUrl)
        {
            Context = context;
            RtspChannelUrl = rtspChannelUrl;
        }

        public Guid Id { get { return Context?.ConnectionInfo.Id ?? Guid.Empty; } }

        public bool IsConnected { get { return Context?.IsAvailable ?? false; } }

        internal void Close()
        {
            try
            {
                if(IsConnected)
                    Context?.Close();
            }
            catch (Exception)
            {
            }
        }

        internal static bool IsNullOrEmpty(ClientSession client)
        {
            return client == null;
        }
    }
}
