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

        public void AddServer(string address, int port)
        {
            Servers.Add(new Server { Addess = address, Port = port });
        }

        public MemcachedClientOptions Value
        {
            get
            {
                return this;
            }
        }
    }

    public class Server
    {
        public string Addess { get; set; }
        public int Port { get; set; }
    }
}
