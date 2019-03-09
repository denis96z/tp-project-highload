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
        public string BasePath;
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

            var threadIndex = 0;
            
            while (true)
            {
                var socket = listener.Accept();
                
                while (true)
                {
                    if (threadIndex == _serverInfo.NumThreads)
                    {
                        threadIndex = 0;
                    }
                    
                    if (_threadInfos[threadIndex].IsSet)
                    {
                        ++threadIndex;
                        continue;
                    }

                    _threadInfos[threadIndex].IsSet = true;
                    _threadInfos[threadIndex].Socket = socket;
                    _threadInfos[threadIndex].EventHandle.Set();

                    ++threadIndex;
                    break;
                }
            }
        }

        private void WorkerFunc(object i)
        {
            var workerIndex = (int) i;

            var ok = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 12\r\n\r\nHello World!\r\n\r\n");

            const int reqBufferLen = 256;
            var reqBuffer = new byte[reqBufferLen];

            const int pathBufferLen = 128;
            var pathBuffer = new byte[pathBufferLen];
            
            while (true)
            {
                _threadInfos[workerIndex].EventHandle.WaitOne();

                var socket = _threadInfos[workerIndex].Socket;

                socket.Receive(reqBuffer);
                if (reqBuffer[0] == 'H')
                {
                    var pathLen = ParsePath(pathBuffer, reqBuffer, 5);
                    //Console.WriteLine(Encoding.UTF8.GetString(pathBuffer, 0, pathLen));
                }
                else if (reqBuffer[0] == 'G')
                {
                    var pathLen = ParsePath(pathBuffer, reqBuffer, 4);
                    //Console.WriteLine(Encoding.UTF8.GetString(pathBuffer, 0, pathLen));
                }
                /*else
                {
                    
                }*/

                socket.Send(ok);
                socket.Close();

                _threadInfos[workerIndex].EventHandle.Reset();
                _threadInfos[workerIndex].IsSet = false;
            }
        }

        private static int ParsePath(byte[] pathBuffer, byte[] buffer, int startIndex)
        {
            var pathLen = 0;
            
            for (var curIndex = startIndex; curIndex < buffer.Length; ++curIndex)
            {
                var current = buffer[curIndex];

                if (current == '+')
                {
                    buffer[curIndex] = ((byte) ' ');
                    ++curIndex;
                }
                /*else if (current == '%')
                {
                    var a = ConvertHexByte(buffer[curIndex + 1]) * 10; 
                    var b = ConvertHexByte(buffer[curIndex + 2]);
                    curIndex += 3;
                }*/
                else if (current == ' ')
                {
                    return pathLen;
                }

                pathBuffer[pathLen++] = current;
            }

            throw new Exception("request buffer end");
        }

        private static int ConvertHexByte(int b)
        {
            if (b >= '0' && b <= '9')
            {
                Console.WriteLine((byte)'0');
                b -= (byte) '0';
            }
            else if (b >= 'a' && b <= 'f')
            {
                b -= (byte) 'a';
            }
            else
            {
                b -= (byte) 'A';
            }

            return b;
        }
    }
}