using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            const string okHeader = "HTTP/1.1 200 OK\r\n";
            const string notFoundHeader = "HTTP/1.1 404 Not Found\r\n";
            const string forbiddenHeader = "HTTP/1.1 403 Forbidden\r\n";
            const string notAllowedHeader = "HTTP/1.1 405 Method Not Allowed\r\n";
            
            const string srvHeader = "Server: srv\r\n";
            const string connectionHeader = "Connection: Close\r\n";

            const string contentLength = "Content-Length: ";
            const string contentLengthZeroHeader = contentLength + "0" +"\r\n";

            const string contentType = "Content-Type: ";
            const string htmlHeader = contentType + "text/html" + "\r\n";
            const string cssHeader = contentType + "text/css" + "\r\n";
            const string jsHeader = contentType + "application/javascript" + "\r\n";
            const string jpegHeader = contentType + "image/jpeg" + "\r\n";
            const string pngHeader = contentType + "image/png" + "\r\n";
            const string gifHeader = contentType + "image/gif" + "\r\n";
            const string swfHeader = contentType + "application/x-shockwave-flash" + "\r\n";

            byte[] MakeOk(string contentTypeHeader)
            {
                return Encoding.UTF8.GetBytes(okHeader + srvHeader + connectionHeader + contentTypeHeader);
            }

            var okHtml = MakeOk(htmlHeader);
            var okCss = MakeOk(cssHeader);
            var okJs = MakeOk(jsHeader);
            var okJpeg = MakeOk(jpegHeader);
            var okPng = MakeOk(pngHeader);
            var okGif = MakeOk(gifHeader);
            var okSwf = MakeOk(swfHeader);

            var endLine = Encoding.UTF8.GetBytes("\r\n");
            var doubleEndLine = Encoding.UTF8.GetBytes("\r\n\r\n");

            (string, bool, bool) MakePath(byte[] buffer, int pathLen)
            {
                var path = Encoding.UTF8.GetString(buffer, 0, pathLen);

                if (path.Contains("../"))
                {
                    return (null, false, false);
                }

                var isIndex = false;
                
                if (path[path.Length - 1] == '/')
                {
                    path = $"{path}index.html";
                    isIndex = true;
                }
                
                Console.WriteLine($"PATH: {path}\nRAW:"); //TODO remove log

                return (path, true, isIndex);
            }

            byte[] SelectOkResponse(string ext)
            {
                switch (ext)
                {                    
                    case ".html":
                        return okHtml;
                    
                    case ".css":
                        return okCss;
                    
                    case ".js":
                        return okJs;
                    
                    case ".jpeg":
                    case ".jpg":
                        return okJpeg;
                        
                    case ".png":
                        return okPng;
                    
                    case ".gif":
                        return okGif;
                    
                    case ".swf":
                        return okSwf;
                    
                    default:
                        return okHtml; //TODO text/plain
                }
            }

            var notFound = Encoding.UTF8.GetBytes(notFoundHeader + srvHeader +
                                                  connectionHeader + contentLengthZeroHeader);
            var forbidden = Encoding.UTF8.GetBytes(forbiddenHeader + srvHeader +
                                                   connectionHeader + contentLengthZeroHeader);
            var notAllowed = Encoding.UTF8.GetBytes(notAllowedHeader + srvHeader +
                                                    connectionHeader + contentLengthZeroHeader);

            const int reqBufferLen = 256;
            var reqBuffer = new byte[reqBufferLen];

            const int pathBufferLen = 128;
            var pathBuffer = new byte[pathBufferLen];

            while (true)
            {
                _threadInfos[workerIndex].EventHandle.WaitOne();

                var socket = _threadInfos[workerIndex].Socket;

                void SendDate()
                {
                    var date = DateTime.Now.ToUniversalTime().ToString("R"); //TODO optimize
                    socket.Send(Encoding.UTF8.GetBytes($"Date: {date}\r\n")); //TODO optimize
                }

                void SendContentLength(long value)
                {
                    socket.Send(Encoding.UTF8.GetBytes($"Content-Length: {value}\r\n")); //TODO optimize
                }

                var reqLen = socket.Receive(reqBuffer);
                Console.WriteLine(Encoding.UTF8.GetString(reqBuffer, 0, reqLen)); //TODO remove log
                
                if (reqBuffer[0] == 'H')
                {
                    var pathLen = ParsePath(pathBuffer, reqBuffer, 5);
                    
                    var (path, valid, isIndex) = MakePath(pathBuffer, pathLen);
                    if (!valid)
                    {
                        socket.Send(forbidden);
                        SendDate();
                        socket.Send(endLine);
                    }
                    else
                    {
                        var fileInfo = new FileInfo(_serverInfo.BasePath + path);
                        if (!fileInfo.Exists)
                        {
                            if (isIndex)
                            {
                                socket.Send(forbidden);
                                SendDate();
                                socket.Send(endLine);
                            }
                            else
                            {
                                socket.Send(notFound);
                                SendDate();
                                socket.Send(endLine);
                            }
                        }
                        else
                        {
                            socket.Send(SelectOkResponse(fileInfo.Extension));
                            SendContentLength(fileInfo.Length);
                            SendDate();
                            socket.Send(endLine);
                        }
                    }
                }
                else if (reqBuffer[0] == 'G')
                {
                    var pathLen = ParsePath(pathBuffer, reqBuffer, 4);
                    
                    var (path, valid, isIndex) = MakePath(pathBuffer, pathLen);
                    if (!valid)
                    {
                        socket.Send(forbidden);
                        SendDate();
                        socket.Send(endLine);
                    }
                    else
                    {
                        var fileInfo = new FileInfo(_serverInfo.BasePath + path);
                        if (!fileInfo.Exists)
                        {
                            if (isIndex)
                            {
                                socket.Send(forbidden);
                                SendDate();
                                socket.Send(endLine);
                            }
                            else
                            {
                                socket.Send(notFound);
                                SendDate();
                                socket.Send(endLine);
                            }
                        }
                        else
                        {
                            socket.Send(SelectOkResponse(fileInfo.Extension));
                            SendContentLength(fileInfo.Length);
                            SendDate();
                            socket.Send(endLine);
                            socket.SendFile(fileInfo.FullName);
                            socket.Send(doubleEndLine);
                        }
                    }
                }
                else
                {
                    socket.Send(notAllowed);
                    SendDate();
                    socket.Send(endLine);
                }
                
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
                else if (current == '%')
                {
                    var a = ConvertHexByte(buffer[curIndex + 1]) * 16; 
                    var b = ConvertHexByte(buffer[curIndex + 2]);
                    
                    current = (byte)(a + b);
                    curIndex += 2;
                }
                else if (current == ' ' || current == '?')
                {
                    return pathLen;
                }

                pathBuffer[pathLen++] = current;
            }

            return -1;
        }

        private static int ConvertHexByte(int b)
        {
            if (b >= '0' && b <= '9')
            {
                b -= (byte) '0';
            }
            else if (b >= 'a' && b <= 'f')
            {
                b -= (((byte) 'a') - 10);
            }
            else
            {
                b -= (((byte) 'A') - 10);
            }

            return b;
        }
    }
}