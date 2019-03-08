using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace HttpStaticServer.HttpServer
{
    public struct ServerInfo
    {
        public int Port;
        public int NumThreads;
    }

    public class Server
    {
        private struct ThreadInfo
        {
            public volatile bool IsSet;
            public EventWaitHandle EventHandle;
            public Socket Socket;
        }

        private readonly ServerInfo _serverInfo;
        private readonly ThreadInfo[] _threadInfos;

        public Server(ServerInfo serverInfo)
        {
            _serverInfo = serverInfo;
            _threadInfos = new ThreadInfo[serverInfo.NumThreads];
        }

        private const int ListenerBacklog = 1024;

        public void Run()
        {
            var listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            listener.Bind(new IPEndPoint(IPAddress.Any, _serverInfo.Port));
            listener.Listen(ListenerBacklog);

            for (var i = 0; i < _serverInfo.NumThreads; ++i)
            {
                _threadInfos[i] = new ThreadInfo()
                {
                    IsSet = false,
                    EventHandle = new EventWaitHandle(false, EventResetMode.ManualReset)
                };
                new Thread(WorkerFunc).Start(i);
            }

            while (true)
            {
                var socket = listener.Accept();
                var isHandled = false;
                
                while (!isHandled)
                {
                    for (var i = 0; i < _serverInfo.NumThreads; ++i)
                    {
                        if (_threadInfos[i].IsSet) continue;

                        _threadInfos[i].IsSet = true;
                        _threadInfos[i].Socket = socket;
                        _threadInfos[i].EventHandle.Set();

                        isHandled = true;
                        break;
                    }
                }
            }
        }

        private void WorkerFunc(object i)
        {
            var workerIndex = (int) i;

            var ok = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 12\r\n\r\nHello World!\r\n\r\n");

            const int bufferLen = 512;
            var reqBuffer = new byte[bufferLen];

            while (true)
            {
                _threadInfos[workerIndex].EventHandle.WaitOne();

                var socket = _threadInfos[workerIndex].Socket;

                socket.Send(ok);
                socket.Close();

                _threadInfos[workerIndex].EventHandle.Reset();
                _threadInfos[workerIndex].IsSet = false;
            }
        }

        private static string ParsePath(IReadOnlyList<byte> buffer, int startIndex)
        {
            var pathBuilder = new StringBuilder();

            for (var i = startIndex; i < buffer.Count; ++i)
            {
                var current = buffer[i];

                if (current == ' ')
                {
                    return pathBuilder.ToString();
                }

                pathBuilder.Append(Convert.ToChar(current));

                ++i;
            }

            throw new Exception("request buffer end");
        }
    }
}