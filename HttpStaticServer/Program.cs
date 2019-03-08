using System;
using HttpStaticServer.HttpServer;

namespace HttpStaticServer
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            new Server(new ServerInfo()
            {
                Port = 8080,
                NumThreads = 64,
            }).Run();
        }
    }
}