using Enyim.Caching.Memcached;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Enyim.Caching.Configuration
{
    public class MemcachedClientOptions : IOptions<MemcachedClientOptions>
    {
        public MemcachedProtocol Protocol { get; set; } = MemcachedProtocol.Binary;

        public SocketPoolConfiguration SocketPool { get; set; } = new SocketPoolConfiguration();

        public List<Server> Servers { get; set; } = new List<Server>();

        public Authentication Authentication { get; set; }

        public string KeyTransformer { get; set; }

        public ITranscoder Transcoder { get; set; }

        public MemcachedClientOptions Value => this;

        public void AddServer(string address, int port)
        {
            Servers.Add(new Server { Address = address, Port = port });
        }

        public void AddPlainTextAuthenticator(string zone, string userName, string password)
        {
            Authentication = new Authentication
            {
                Type = typeof(PlainTextAuthenticator).ToString(),
                Parameters = new Dictionary<string, string>
                {
                    { $"{nameof(zone)}", zone },
                    { $"{nameof(userName)}", userName},
                    { $"{nameof(password)}", password}
                }
            };
        }
    }

    public class Server
    {
        public string Address { get; set; }
        public int Port { get; set; }
    }

    public class Authentication
    {
        public string Type { get; set; }

        public Dictionary<string, string> Parameters { get; set; }
    }
}
