namespace LiveStreamingWebRTC
{
    public class RtspChannelInfo
    {
        public string Url { get; private set; }
        public string ChannelNumber { get; private set; }


        public RtspChannelInfo(string url, string channelNumber)
        {
            Url = url ?? string.Empty;
            ChannelNumber = channelNumber ?? string.Empty;
        }
    }
}