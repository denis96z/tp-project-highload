using System.IO;
using System.Collections.Generic;
using HttpStaticServer.HttpServer;

namespace HttpStaticServer.Config
{
    public class ConfigReader
    {
        private const string ConfigFilename = "/etc/httpd.conf";

        public ServerInfo GetConfig()
        {
            var lines = File.ReadAllLines(ConfigFilename);

            var pPort = GetPropertyValue(lines, "port", "80");
            var pThreadLimit = GetPropertyValue(lines, "thread_limit", "256");
            var pDocumentRoot = GetPropertyValue(lines, "document_root", "/var/www/html");

            var config = new ServerInfo()
            {
                Port = int.Parse(pPort),
                NumThreads = int.Parse(pThreadLimit),
                BasePath = pDocumentRoot
            };

            var rootDirInfo = new FileInfo(pDocumentRoot);
            if (!rootDirInfo.Exists)
            {
                throw new DirectoryNotFoundException(rootDirInfo.ToString());
            }

            return config;
        }

        private static string GetPropertyValue(IEnumerable<string> lines, string name, string defaultValue)
        {
            var startIndex = name.Length + 1;
            
            foreach (var line in lines)
            {
                if (!line.StartsWith(name)) continue;

                var index = line.IndexOf(' ', startIndex);
                if (index == -1)
                {
                    continue;
                }

                return line.Substring(startIndex, index - startIndex);
            }

            return defaultValue;
        }
    }
}