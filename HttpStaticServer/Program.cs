using System;
using HttpStaticServer.Config;
using HttpStaticServer.HttpServer;

namespace HttpStaticServer
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var config = ConfigReader.GetConfig();
            new Server(config).Run();
        }
    }
}