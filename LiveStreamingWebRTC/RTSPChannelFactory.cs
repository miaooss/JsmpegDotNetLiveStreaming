using Fleck;
using LiveStreamingWebRTC.Logger;

namespace LiveStreamingWebRTC
{
    public interface IRTSPChannelFactory
    {
        RtspChannelInfo DecodeChannelFromClient(IWebSocketConnection context);
        IRTSPChannelListener Create(RtspChannelInfo rtspChannelInfo);
    }

    public class RTSPChannelFactory : IRTSPChannelFactory
    {
        private readonly ILogger logger;

        public RTSPChannelFactory(ILogger logger)
        {
            this.logger = logger;
        }

        public RtspChannelInfo DecodeChannelFromClient(IWebSocketConnection context)
        {
            if(context.ConnectionInfo.Path.Trim().Length > 1)
                return new RtspChannelInfo(context.ConnectionInfo.Path.Replace(@"/?rtsplink=", string.Empty), context.ConnectionInfo.Path.Substring(context.ConnectionInfo.Path.Length - 1));
            return new RtspChannelInfo(string.Empty, string.Empty);
        }

        public IRTSPChannelListener Create(RtspChannelInfo rtspChannelInfo)
        {
            return new RTSPChannelListener(rtspChannelInfo.Url, 352, 240, logger);
        }
    }
}