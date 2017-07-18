using LiveStreamingWebRTC.Logger;
using LiveStreamingWebRTC.Utils;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace LiveStreamingWebRTC
{
    public interface IRTSPChannelListener : IDisposable
    {
        string RtspChannelUrl { get; }
        int Width { get; }
        int Height { get; }
        int ClientCount { get; }

        bool AddClient(ClientSession client);
        void Start();
        void Stop();
        bool RemoveClient(ClientSession client);
        void ClearClient();
    }

    public class RTSPChannelListener : IRTSPChannelListener
    {
        private const string FFMPEG_ARGUMENT = "-re -rtsp_transport udp -timeout -1 -i {0} -video_size {1}x{2} -f mpegts -codec:v mpeg1video -bf 0 -codec:a mp2 -b:v 800k -r 30 {3}://127.0.0.1:{4}";
        private readonly ConcurrentDictionary<Guid, ClientSession> clientList;
        private readonly ILogger logger;
        private Process ffmpeg;
        private Socket ffmpeStream;
        private ProtocolType protocolType = ProtocolType.Udp;
        public string RtspChannelUrl { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public RTSPChannelListener(string rtspChannelUrl, int width, int height, ILogger logger)
        {
            ffmpeStream = CreateSocket();
            RtspChannelUrl = rtspChannelUrl;
            Width = width;
            Height = height;
            this.logger = logger;
            clientList = new ConcurrentDictionary<Guid, ClientSession>();
        }

        public int ClientCount { get { return clientList.Count; } }

        protected ProtocolType ProtocolType { get { return protocolType == ProtocolType.Udp ? ProtocolType.Udp : ProtocolType.Tcp; } }
        protected string ProtocolTypeStr { get { return ProtocolType.ToString().ToLower(); } }

        protected Socket CreateSocket()
        {
            Socket netWorkSocket;
            if (ProtocolType == ProtocolType.Udp)
                netWorkSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            else
                netWorkSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            netWorkSocket.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0));
            return netWorkSocket;
        }

        public bool AddClient(ClientSession client)
        {
            if (ClientSession.IsNullOrEmpty(client)) throw new ArgumentNullException("client");
            if (client.RtspChannelUrl != RtspChannelUrl) throw new ArgumentException("The Client does not belong to this channel");
            return !clientList.ContainsKey(client.Id) && clientList.TryAdd(client.Id, client);
        }


        public override bool Equals(object obj)
        {
            var localObj = obj as RTSPChannelListener;
            return localObj != null && localObj.RtspChannelUrl == RtspChannelUrl;
        }

        public override int GetHashCode()
        {
            return (RtspChannelUrl ?? string.Empty).GetHashCode();
        }

        public void Start()
        {
            if (ffmpeg != null) return;
            logger.Info($"Start channel {RtspChannelUrl}");
            StartReceiving();
            var args = string.Format(FFMPEG_ARGUMENT, RtspChannelUrl, Width, Height, ProtocolTypeStr, ((IPEndPoint)ffmpeStream.LocalEndPoint).Port);
            //Use Jmpeg to convert the rtspSource
            ffmpeg = new Process
            {
                StartInfo = {
                    FileName = Path.Combine(Environment.CurrentDirectory, "ffmpeg.exe"),
                    Arguments = args,
#if DEBUG
                    UseShellExecute = true,
#else
                    UseShellExecute = false,
#endif
                    //RedirectStandardOutput = true,
                    //StandardOutputEncoding = Encoding.Default,
                    //StandardErrorEncoding  = Encoding.Default,
                    //RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Environment.CurrentDirectory
                }
            };

            ffmpeg.EnableRaisingEvents = true;
            //ffmpeg.OutputDataReceived += (s, e) => OnData(e.Data);
            //ffmpeg.ErrorDataReceived += (s, e) => OnError($@"Error: {e.Data}");
            ffmpeg.Exited += (s, e) => OnExited(s);
            ffmpeg.Start();
            //ffmpeg.BeginOutputReadLine();
            //ffmpeg.BeginErrorReadLine();
        }

        private void StartReceiving()
        {
            if (protocolType == ProtocolType.Udp)
                Receive(ffmpeStream);
            else
                Accept(ffmpeStream);
        }

        private void Receive(Socket client, StateObject state = null)
        {
            try
            {
                // Create the state object.
                if (state == null)
                {
                    state = new StateObject();
                    state.workSocket = client;
                }

                // Begin receiving the data from the remote device.
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                logger.Error(e.ToString());
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket 
                // from the asynchronous state object.
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;

                // Read data from the remote device.
                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.
                    //state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
                    var data = bytesRead < StateObject.BufferSize ? state.buffer.ResizeArray(0, bytesRead) : state.buffer;
                    BroadCast(data);
                    // Get the rest of the data.
                    Receive(client, state);
                }
                else
                {
                    // All the data has arrived; put it in response.
                    //if (state.sb.Length > 1)
                    //{
                    //    response = state.sb.ToString();
                    //}
                    //// Signal that all bytes have been received.
                    //receiveDone.Set();
                }
            }
            catch (Exception e)
            {
                logger.Error(e.ToString());
            }
        }

        private void BroadCast(byte[] data)
        {
            if (data == null) return;
            foreach (var client in clientList.Values)
            {
                try
                {
                    client.Context.Send(data);
                }
                catch (Exception ex)
                {
                }
            }
        }

        private void Accept(Socket server)
        {
            try
            {
                if(!server.Connected)
                    server.Listen(1000);
                server.BeginAccept(new AsyncCallback(AcceptCallback), server);
            }
            catch (Exception e)
            {
                logger.Error(e.ToString());
            }
        }

        public void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.
            //allDone.Set();

            // Get the socket that handles the client request.
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            Receive(handler);
        }

        private void OnExited(object sender)
        {
            if (ffmpeg == null || ffmpeg != sender) return;
            ffmpeg = null;
        }

        public void Stop()
        {
            if (ffmpeg == null) return;
            try
            {
                ffmpeg.Kill();
                ffmpeg.Dispose();
            }
            catch (Exception ex)
            {
            }
            finally
            {
                ffmpeg = null;
            }
        }

        public bool RemoveClient(ClientSession client)
        {
            ClientSession clientRemoved;
            return clientList.ContainsKey(client.Id) && clientList.TryRemove(client.Id, out clientRemoved);
        }

        public void ClearClient()
        {
            clientList.Clear();
        }

        // State object for reading client data asynchronously
        public class StateObject
        {
            // Client  socket.
            public Socket workSocket = null;
            // Size of receive buffer.
            public const int BufferSize = 1472;
            // Receive buffer.
            public byte[] buffer = new byte[BufferSize];
        }

#region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Stop();
                    ClearClient();
                    try
                    {
                        if (ffmpeStream != null)
                            ffmpeStream.Close();
                    }
                    catch (Exception ex)
                    {
                    }
                    finally
                    {
                        ffmpeStream = null;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~RTSPChannelListener() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
#endregion
    }
}