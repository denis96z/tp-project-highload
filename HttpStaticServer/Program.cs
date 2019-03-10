using System;
using HttpStaticServer.Config;
using HttpStaticServer.HttpServer;

namespace HttpStaticServer
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var configFilename = ConfigReader.DefaultConfigFilename;  
            if (args.Length > 0)
            {
                configFilename = args[0];
            }
            
            var config = ConfigReader.GetConfig(configFilename);
            Console.WriteLine($"starting server on 0.0.0.0:{config.Port}");
            
            new Server(config).Run();
        }
    }
}