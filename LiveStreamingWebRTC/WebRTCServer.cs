using System;
using System.Collections.Concurrent;
using System.Linq;
using Fleck;
using LiveStreamingWebRTC.Message;
using LiveStreamingWebRTC.Exceptions;
using LiveStreamingWebRTC.Logger;

namespace LiveStreamingWebRTC
{
    public class WebRTCServer : IDisposable
    {
        private readonly ConcurrentDictionary<Guid, ClientSession> clientList;
        private readonly ConcurrentDictionary<string, IRTSPChannelListener> rtspChannelList;
        private readonly IRTSPChannelFactory rtspChannelFactory;
        private readonly IWebRTCMessageFactory messageFactory;
        private readonly ILogger logger;

        private WebSocketServer server;
        private int clientLimitParChannel;

        public WebRTCServer(IRTSPChannelFactory rtspChannelFactory, IWebRTCMessageFactory messageFactory, ILogger logger)
        {
            this.messageFactory = messageFactory;
            this.rtspChannelFactory = rtspChannelFactory;
            this.clientList = new ConcurrentDictionary<Guid, ClientSession>();
            this.rtspChannelList = new ConcurrentDictionary<string, IRTSPChannelListener>();
            this.logger = logger;
        }

        public bool IsStarted { get { return server != null; } }

        public void Start(int port)
        {
            Start("ws://0.0.0.0:" + port, 10);
        }

        public void Start(int port, int clientLimitParChannel)
        {
            Start("ws://0.0.0.0:" + port, clientLimitParChannel);
        }

        public void Start(string URL, int clientLimitParChannel)
        {
            this.clientLimitParChannel = clientLimitParChannel;
            server = new WebSocketServer(URL)
            {
                SupportedSubProtocols = new [] { "none" }
            };
            server.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    try
                    {
                        OnConnected(socket);
                    }
                    catch (Exception ex)
                    {
                        this.logger.Info($"OnConnected: {ex}");
                    }
                };
                socket.OnMessage = message =>
                {
                    try
                    {
                        OnReceive(socket, message);
                    }
                    catch (Exception ex)
                    {
                        this.logger.Info($"OnReceive: {ex}");
                    }
                };
                socket.OnClose = () =>
                {
                    try
                    {
                        OnDisconnect(socket);
                    }
                    catch (Exception ex)
                    {
                        this.logger.Info($"OnDisconnect: {ex}");
                    }
                };
                socket.OnError = (e) =>
                {
                    try
                    {
                        OnDisconnect(socket);
                        socket.Close();
                    }
                    catch (Exception ex)
                    {
                        this.logger.Info($"OnError: {ex}");
                    }
                };
            });
        }


        private void OnConnected(IWebSocketConnection context)
        {
            var rtspChannelInfo = rtspChannelFactory.DecodeChannelFromClient(context);
            logger.Info($"New Client for {rtspChannelInfo.Url}");
            var clientSession = new ClientSession(context, rtspChannelInfo.Url);
            IRTSPChannelListener channel;
            if (!rtspChannelList.TryGetValue(rtspChannelInfo.Url, out channel))
            {
                channel = rtspChannelFactory.Create(rtspChannelInfo);
                if (rtspChannelList.TryAdd(channel.RtspChannelUrl, channel))
                    channel.Start();
            }
            try
            {
                if (channel.ClientCount + 1 > clientLimitParChannel)
                    throw new ClientLimitReachedException($"Client limit reached for channel {channel.RtspChannelUrl}");
                channel.AddClient(clientSession);
                clientList.TryAdd(clientSession.Id, clientSession);
                SendStringMessage(context, messageFactory.CreateInitialMessage(channel.Width, channel.Height));
            }
            catch (ClientLimitReachedException ex)
            {
                SendStringMessage(context, messageFactory.CreateMessage(EnumMessage.TooManyClientConnected));
                OnDisconnect(context);
                context.Close();
            }
        }

        private void SendStringMessage(IWebSocketConnection context, string message)
        {
            try
            {
                context?.Send(message);
            }
            catch (Exception ex)
            {

            }
        }

        private void OnDisconnect(IWebSocketConnection context)
        {
            ClientSession client;
            IRTSPChannelListener channel;
            if (clientList.TryRemove(context.ConnectionInfo.Id, out client)
                && rtspChannelList.TryGetValue(client.RtspChannelUrl, out channel)
                && channel.RemoveClient(client)
                && channel.ClientCount == 0
                && rtspChannelList.TryRemove(client.RtspChannelUrl, out channel))
            {
                channel.Dispose();
                logger.Info($"Close channel {client.RtspChannelUrl}");
            }
        }

        private void OnReceive(IWebSocketConnection context, string msg)
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.Collect();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            try
            {
                server.Dispose();
                foreach (var channel in rtspChannelList.Values)
                {
                    try
                    {
                        channel.Dispose();
                    }
                    catch (Exception ex)
                    {
                    }
                }

                foreach (var client in clientList.Values.Where(cl => cl.IsConnected))
                {
                    try
                    {
                        client.Close();
                    }
                    catch (Exception ex)
                    {
                    }
                }

                clientList.Clear();
                rtspChannelList.Clear();
            }
            catch { }
        }

    }
}
