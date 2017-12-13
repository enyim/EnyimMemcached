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

        public SocketPoolOptions SocketPool { get; set; }

        public List<Server> Servers { get; set; } = new List<Server>();

        public Authentication Authentication { get; set; }

        public string KeyTransformer { get; set; }

        public ITranscoder Transcoder { get; set; }

        public IProviderFactory<IMemcachedNodeLocator> NodeLocatorFactory { get; set; }

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

    public class SocketPoolOptions
    {
        public int MinPoolSize { get; set; } = 10;
        public int MaxPoolSize { get; set; } = 20;
        public TimeSpan ConnectionTimeout { get; set; } = new TimeSpan(0, 0, 10);
        public TimeSpan ReceiveTimeout { get; set; } = new TimeSpan(0, 0, 10);
        public TimeSpan DeadTimeout { get; set; } = new TimeSpan(0, 0, 10);
        public TimeSpan QueueTimeout { get; set; } = new TimeSpan(0, 0, 0, 0, 100);

        public void CheckTimeout()
        {
            CheckTimeout(nameof(ConnectionTimeout), ConnectionTimeout);
            CheckTimeout(nameof(ReceiveTimeout), ReceiveTimeout);
            CheckTimeout(nameof(DeadTimeout), DeadTimeout);
            CheckTimeout(nameof(QueueTimeout), QueueTimeout);
        }

        private void CheckTimeout(string paramName, TimeSpan value)
        {
            if (value.TotalHours > 1)
            {
                throw new ArgumentOutOfRangeException(paramName, $"{paramName} must be <= 1 hour");
            }
        }
    }
}
