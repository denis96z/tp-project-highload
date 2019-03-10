using System.IO;
using System.Collections.Generic;
using HttpStaticServer.HttpServer;

namespace HttpStaticServer.Config
{
    public static class ConfigReader
    {
        public const string DefaultConfigFilename = "/etc/httpd.conf";

        public static ServerInfo GetConfig(string configFilename)
        {
            var lines = File.ReadAllLines(configFilename);

            var pPort = GetPropertyValue(lines, "port", "80");
            var pThreadLimit = GetPropertyValue(lines, "thread_limit", "256");
            var pDocumentRoot = GetPropertyValue(lines, "document_root", "/var/www/html");

            var config = new ServerInfo()
            {
                Port = int.Parse(pPort),
                NumThreads = int.Parse(pThreadLimit),
                BasePath = pDocumentRoot
            };

            if (!Directory.Exists(pDocumentRoot))
            {
                throw new DirectoryNotFoundException(pDocumentRoot);
            }

            return config;
        }

        private static string GetPropertyValue(IEnumerable<string> lines, string name, string defaultValue)
        {
            var startIndex = name.Length + 1;
            
            foreach (var line in lines)
            {
                if (!line.StartsWith(name)) continue;

                for (; line[startIndex] == ' '; startIndex++) ;

                var index = line.IndexOf(' ', startIndex);

                return index == -1 ? line.Substring(startIndex) :
                    line.Substring(startIndex, index - startIndex);
            }

            return defaultValue;
        }
    }
}