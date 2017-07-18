using System;

namespace LiveStreamingWebRTC.Message
{
    public interface IWebRTCMessageFactory
    {
        string CreateMessage(EnumMessage messageType);
        string CreateInitialMessage(float width, float height);
    }

    public class WebRTCMessageFactory : IWebRTCMessageFactory
    {
        public string CreateInitialMessage(float width, float height)
        {
            var value = new { Action = "Init", Width = width, Height = height };
            return Newtonsoft.Json.JsonConvert.SerializeObject(value, Newtonsoft.Json.Formatting.None);
        }

        public string CreateMessage(EnumMessage messageType)
        {
            switch (messageType)
            {
                case EnumMessage.TooManyClientConnected:
                    var value = new { Action = "Message", Message = "Too many client connected" };
                    return Newtonsoft.Json.JsonConvert.SerializeObject(value, Newtonsoft.Json.Formatting.None);
                default:
                    return string.Empty;
            }
        }
    }
}