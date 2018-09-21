using System;
using System.Collections.Generic;
using System.Net;
using Enyim.Caching.Memcached;
using Enyim.Reflection;
using Enyim.Caching.Memcached.Protocol.Binary;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Net.Sockets;

namespace Enyim.Caching.Configuration
{
    /// <summary>
    /// Configuration class
    /// </summary>
    public class MemcachedClientConfiguration : IMemcachedClientConfiguration
    {
        // these are lazy initialized in the getters
        private Type nodeLocator;
        private ITranscoder _transcoder;
        private IMemcachedKeyTransformer _keyTransformer;
        private ILogger<MemcachedClientConfiguration> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:MemcachedClientConfiguration"/> class.
        /// </summary>
        public MemcachedClientConfiguration(
            ILoggerFactory loggerFactory,
            IOptions<MemcachedClientOptions> optionsAccessor,
            IConfiguration configuration = null,
            ITranscoder transcoder = null,
            IMemcachedKeyTransformer keyTransformer = null)
        {
            if (optionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(optionsAccessor));
            }

            _logger = loggerFactory.CreateLogger<MemcachedClientConfiguration>();

            var options = optionsAccessor.Value;
            if ((options == null || options.Servers.Count == 0) && configuration != null)
            {
                var section = configuration.GetSection("enyimMemcached");
                if (section.Exists())
                {
                    section.Bind(options);
                }
                else
                {
                    _logger.LogWarning($"No enyimMemcached setting in appsetting.json. Use default configuration");
                    options.AddDefaultServer();
                }
            }

            ConfigureServers(options);

            SocketPool = new SocketPoolConfiguration();
            if (options.SocketPool != null)
            {
                options.SocketPool.CheckPoolSize();
                options.SocketPool.CheckTimeout();

                SocketPool.MinPoolSize = options.SocketPool.MinPoolSize;
                _logger.LogInformation($"{nameof(SocketPool.MinPoolSize)}: {SocketPool.MinPoolSize}");

                SocketPool.MaxPoolSize = options.SocketPool.MaxPoolSize;
                _logger.LogInformation($"{nameof(SocketPool.MaxPoolSize)}: {SocketPool.MaxPoolSize}");

                SocketPool.ConnectionTimeout = options.SocketPool.ConnectionTimeout;
                _logger.LogInformation($"{nameof(SocketPool.ConnectionTimeout)}: {SocketPool.ConnectionTimeout}");

                SocketPool.ReceiveTimeout = options.SocketPool.ReceiveTimeout;
                _logger.LogInformation($"{nameof(SocketPool.ReceiveTimeout)}: {SocketPool.ReceiveTimeout}");

                SocketPool.DeadTimeout = options.SocketPool.DeadTimeout;
                _logger.LogInformation($"{nameof(SocketPool.DeadTimeout)}: {SocketPool.DeadTimeout}");

                SocketPool.QueueTimeout = options.SocketPool.QueueTimeout;
                _logger.LogInformation($"{nameof(SocketPool.QueueTimeout)}: {SocketPool.QueueTimeout}");
            }

            Protocol = options.Protocol;

            if (options.Authentication != null && !string.IsNullOrEmpty(options.Authentication.Type))
            {
                try
                {
                    var authenticationType = Type.GetType(options.Authentication.Type);
                    if (authenticationType != null)
                    {
                        _logger.LogDebug($"Authentication type is {authenticationType}.");

                        Authentication = new AuthenticationConfiguration();
                        Authentication.Type = authenticationType;
                        foreach (var parameter in options.Authentication.Parameters)
                        {
                            Authentication.Parameters[parameter.Key] = parameter.Value;
                            _logger.LogDebug($"Authentication {parameter.Key} is '{parameter.Value}'.");
                        }
                    }
                    else
                    {
                        _logger.LogError($"Unable to load authentication type {options.Authentication.Type}.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(new EventId(), ex, $"Unable to load authentication type {options.Authentication.Type}.");
                }
            }

            if (!string.IsNullOrEmpty(options.KeyTransformer))
            {
                try
                {
                    var keyTransformerType = Type.GetType(options.KeyTransformer);
                    if (keyTransformerType != null)
                    {
                        KeyTransformer = Activator.CreateInstance(keyTransformerType) as IMemcachedKeyTransformer;
                        _logger.LogDebug($"Use '{options.KeyTransformer}' KeyTransformer");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(new EventId(), ex, $"Unable to load '{options.KeyTransformer}' KeyTransformer");
                }
            }
            else if (keyTransformer != null)
            {
                this._keyTransformer = keyTransformer;
                _logger.LogDebug($"Use KeyTransformer Type : '{keyTransformer.ToString()}'");
            }

            if (NodeLocator == null)
            {
                NodeLocator = options.Servers.Count > 1 ? typeof(DefaultNodeLocator) : typeof(SingleNodeLocator);
            }

            if (!string.IsNullOrEmpty(options.Transcoder))
            {
                try
                {
                    if (options.Transcoder == "BinaryFormatterTranscoder")
                        options.Transcoder = "Enyim.Caching.Memcached.Transcoders.BinaryFormatterTranscoder";

                    var transcoderType = Type.GetType(options.Transcoder);
                    if (transcoderType != null)
                    {
                        Transcoder = Activator.CreateInstance(transcoderType) as ITranscoder;
                        _logger.LogDebug($"Use '{options.Transcoder}'");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(new EventId(), ex, $"Unable to load '{options.Transcoder}'");
                }
            }
            else if (transcoder != null)
            {
                this._transcoder = transcoder;
                _logger.LogDebug($"Use Transcoder Type : '{transcoder.ToString()}'");
            }

            if (options.NodeLocatorFactory != null)
            {
                NodeLocatorFactory = options.NodeLocatorFactory;
            }
        }

        private void ConfigureServers(MemcachedClientOptions options)
        {
            Servers = new List<DnsEndPoint>();
            foreach (var server in options.Servers)
            {
                if (!IPAddress.TryParse(server.Address, out var address))
                {
                    var ip = Dns.GetHostAddresses(server.Address)
                        .FirstOrDefault(i => i.AddressFamily == AddressFamily.InterNetwork)?.ToString();

                    if (ip == null)
                    {
                        _logger.LogError($"Could not resolve host '{server.Address}'.");
                    }
                    else
                    {
                        _logger.LogInformation($"Memcached server address - {server.Address }({ip}):{server.Port}");
                        server.Address = ip;
                    }
                }
                else
                {
                    _logger.LogInformation($"Memcached server address - {server.Address }:{server.Port}");
                }

                Servers.Add(new DnsEndPoint(server.Address, server.Port));
            }
        }

        /// <summary>
        /// Adds a new server to the pool.
        /// </summary>
        /// <param name="address">The address and the port of the server in the format 'host:port'.</param>
        public void AddServer(string address)
        {
            this.Servers.Add(ConfigurationHelper.ResolveToEndPoint(address));
        }

        /// <summary>
        /// Adds a new server to the pool.
        /// </summary>
        /// <param name="address">The host name or IP address of the server.</param>
        /// <param name="port">The port number of the memcached instance.</param>
        public void AddServer(string host, int port)
        {
            this.Servers.Add(new DnsEndPoint(host, port));
        }

        /// <summary>
        /// Gets a list of <see cref="T:IPEndPoint"/> each representing a Memcached server in the pool.
        /// </summary>
        public IList<DnsEndPoint> Servers { get; private set; }

        /// <summary>
        /// Gets the configuration of the socket pool.
        /// </summary>
        public ISocketPoolConfiguration SocketPool { get; private set; }

        /// <summary>
        /// Gets the authentication settings.
        /// </summary>
        public IAuthenticationConfiguration Authentication { get; private set; }

        /// <summary>
        /// Gets or sets the <see cref="T:Enyim.Caching.Memcached.IMemcachedKeyTransformer"/> which will be used to convert item keys for Memcached.
        /// </summary>
        public IMemcachedKeyTransformer KeyTransformer
        {
            get { return this._keyTransformer ?? (this._keyTransformer = new DefaultKeyTransformer()); }
            set { this._keyTransformer = value; }
        }

        /// <summary>
        /// Gets or sets the Type of the <see cref="T:Enyim.Caching.Memcached.IMemcachedNodeLocator"/> which will be used to assign items to Memcached nodes.
        /// </summary>
        /// <remarks>If both <see cref="M:NodeLocator"/> and  <see cref="M:NodeLocatorFactory"/> are assigned then the latter takes precedence.</remarks>
        public Type NodeLocator
        {
            get { return this.nodeLocator; }
            set
            {
                ConfigurationHelper.CheckForInterface(value, typeof(IMemcachedNodeLocator));
                this.nodeLocator = value;
            }
        }

        /// <summary>
        /// Gets or sets the NodeLocatorFactory instance which will be used to create a new IMemcachedNodeLocator instances.
        /// </summary>
        /// <remarks>If both <see cref="M:NodeLocator"/> and  <see cref="M:NodeLocatorFactory"/> are assigned then the latter takes precedence.</remarks>
        public IProviderFactory<IMemcachedNodeLocator> NodeLocatorFactory { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="T:Enyim.Caching.Memcached.ITranscoder"/> which will be used serialize or deserialize items.
        /// </summary>
        public ITranscoder Transcoder
        {
            get { return _transcoder ?? (_transcoder = new DefaultTranscoder()); }
            set { _transcoder = value; }
        }

        /// <summary>
        /// Gets or sets the type of the communication between client and server.
        /// </summary>
        public MemcachedProtocol Protocol { get; set; }

        #region [ interface                     ]

        IList<System.Net.DnsEndPoint> IMemcachedClientConfiguration.Servers
        {
            get { return this.Servers; }
        }

        ISocketPoolConfiguration IMemcachedClientConfiguration.SocketPool
        {
            get { return this.SocketPool; }
        }

        IAuthenticationConfiguration IMemcachedClientConfiguration.Authentication
        {
            get { return this.Authentication; }
        }

        IMemcachedKeyTransformer IMemcachedClientConfiguration.CreateKeyTransformer()
        {
            return this.KeyTransformer;
        }

        IMemcachedNodeLocator IMemcachedClientConfiguration.CreateNodeLocator()
        {
            var f = this.NodeLocatorFactory;
            if (f != null) return f.Create();

            return this.NodeLocator == null
                    ? new SingleNodeLocator()
                    : (IMemcachedNodeLocator)FastActivator.Create(this.NodeLocator);
        }

        ITranscoder IMemcachedClientConfiguration.CreateTranscoder()
        {
            return this.Transcoder;
        }

        IServerPool IMemcachedClientConfiguration.CreatePool()
        {
            switch (this.Protocol)
            {
                case MemcachedProtocol.Text: return new DefaultServerPool(this, new Memcached.Protocol.Text.TextOperationFactory(), _logger);
                case MemcachedProtocol.Binary: return new BinaryPool(this, _logger);
            }

            throw new ArgumentOutOfRangeException("Unknown protocol: " + (int)this.Protocol);
        }

        #endregion
    }
}

#region [ License information          ]
/* ************************************************************
 * 
 *    Copyright (c) 2010 Attila Kisk? enyim.com
 *    
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *    
 *        http://www.apache.org/licenses/LICENSE-2.0
 *    
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *    
 * ************************************************************/
#endregion
