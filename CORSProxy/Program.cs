using System;
using System.Collections.Generic;
using System.Linq;

namespace CORSProxy // Note: actual namespace depends on the project name.
{
    public class Program
    {
        public static string ERROR_TEMPLATE(string error_message) {return String.Format("<html><head><title>{0}</title></head><body><center><h1>{0}</h1></center><hr><center>{1}</center></body></html>", error_message, displayableVersion);}

        public static Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        public static DateTime buildDate = new DateTime(2000, 1, 1).AddDays(version.Build).AddSeconds(version.Revision * 2);
        public static string displayableVersion = $"Dunkelmann-CORS-Proxy/{version} ({buildDate})";

        public static string PUBLIC_KEY = "";

        public static void Main(string[] args)
        {
            Console.WriteLine("Starting server...");
            ProxyServer server = new ProxyServer();
            server.StartServer();
        }
    }
}