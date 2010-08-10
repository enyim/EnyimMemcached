using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Threading;
using Enyim.Caching.Configuration;

namespace Enyim.Caching.Memcached
{
	public class DefaultServerPool : IDisposable, IServerPool
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(DefaultServerPool));

		// holds all of the currently working servers
		List<IMemcachedNode> workingServers;

		private IMemcachedClientConfiguration configuration;
		//		private IMemcachedKeyTransformer keyTransformer;
		private IMemcachedNodeLocator nodeLocator;
		//		private ITranscoder transcoder;
		private IOperationFactory factory;

		public IEnumerable<IMemcachedNode> GetServers()
		{
			return this.workingServers;
		}

		public DefaultServerPool(IMemcachedClientConfiguration configuration)
		{
			if (configuration == null)
				throw new ArgumentNullException("configuration", "Invalid or missing pool configuration. Check if the enyim.com/memcached section or your custom section presents in the app/web.config.");

			this.configuration = configuration;

			//this.keyTransformer = this.configuration.CreateKeyTransformer() ?? new DefaultKeyTransformer();
			//this.transcoder = this.configuration.CreateTranscoder() ?? new DefaultTranscoder();
			this.factory = this.configuration.CreateOperationFactory();

			if (this.factory == null) throw new ArgumentException("Invalid op factory.", "configuration");
		}

		/// <summary>
		/// This will start the pool: initializes the nodelocator, warms up the socket pools, etc.
		/// </summary>
		public void Start()
		{
			// initialize the server list
			this.workingServers = new List<IMemcachedNode>();

			foreach (IPEndPoint ip in this.configuration.Servers)
			{
				var node = this.CreateNode(ip);

				this.workingServers.Add(node);
			}

			// initialize the locator
			var locator = this.configuration.CreateNodeLocator();
			locator.Initialize(this.workingServers);

			this.nodeLocator = locator;
		}

		public IAuthenticator Authenticator { get; set; }

		/// <summary>
		/// Finds the <see cref="T:MemcachedNode"/> which is responsible for the specified item
		/// </summary>
		/// <param name="itemKey"></param>
		/// <returns></returns>
		private IMemcachedNode LocateNode(string itemKey)
		{
			IMemcachedNode node = this.nodeLocator.Locate(itemKey);
			// probably all servers are down
			if (node == null)
			{
				if (log.IsWarnEnabled) log.Warn("Locator did not found a node for " + itemKey);
				return null;
			}

			// it's the locator's responsibility to reeturn only working nodes
			if (!node.IsAlive)
			{
				if (log.IsWarnEnabled) log.Warn("Locator returned a dead node " + node.EndPoint + " for " + itemKey);
				return null;
			}

			return node;
		}

		public virtual PooledSocket Acquire(string itemKey)
		{
			IMemcachedNode server = this.LocateNode(itemKey);

			return server == null ? null : server.Acquire();
		}

		public IDictionary<IMemcachedNode, IList<string>> SplitKeys(IEnumerable<string> keys)
		{
			Dictionary<IMemcachedNode, IList<string>> keysByNode = new Dictionary<IMemcachedNode, IList<string>>(MemcachedNode.Comparer.Instance);

			IList<string> nodeKeys;
			IMemcachedNode node;

			foreach (string key in keys)
			{
				node = this.LocateNode(key);

				if (!keysByNode.TryGetValue(node, out nodeKeys))
				{
					nodeKeys = new List<string>();
					keysByNode.Add(node, nodeKeys);
				}

				nodeKeys.Add(key);
			}

			return keysByNode;
		}

		~DefaultServerPool()
		{
			try { ((IDisposable)this).Dispose(); }
			catch { }
		}

		protected virtual IMemcachedNode CreateNode(IPEndPoint endpoint)
		{
			return new MemcachedNode(endpoint, this.configuration.SocketPool, this.Authenticator);
		}

		#region [ IServerPool                  ]

		//IMemcachedKeyTransformer IServerPool.KeyTransformer
		//{
		//    get { return this.keyTransformer; }
		//}

		//ITranscoder IServerPool.Transcoder
		//{
		//    get { return this.transcoder; }
		//}

		//IMemcachedNodeLocator IServerPool.NodeLocator
		//{
		//    get { return this.nodeLocator; }
		//}

		IEnumerable<IMemcachedNode> IServerPool.GetServers()
		{
			return this.GetServers();
		}

		void IServerPool.Start()
		{
			this.Start();
		}

		IOperationFactory IServerPool.OperationFactory
		{
			get { return this.factory; }
		}

		#endregion
		#region [ IDisposable                  ]
		void IDisposable.Dispose()
		{
			//ReaderWriterLock rwl = this.serverAccessLock;

			//if (Interlocked.CompareExchange(ref this.serverAccessLock, null, rwl) == null)
			//    return;

			GC.SuppressFinalize(this);

			try
			{
				//rwl.UpgradeToWriterLock(Timeout.Infinite);

				Action<IMemcachedNode> cleanupNode = node =>
				{
					//node.SocketConnected -= this.OnSocketConnected;
					node.Dispose();
				};

				// dispose the nodes (they'll kill conenctions, etc.)
				//this.deadServers.ForEach(cleanupNode);
				this.workingServers.ForEach(cleanupNode);

				//this.deadServers.Clear();
				this.workingServers.Clear();

				var nd = this.nodeLocator as IDisposable;
				if (nd != null)
					nd.Dispose();

				this.nodeLocator = null;

				//this.isAliveTimer.Dispose();
				//this.isAliveTimer = null;
			}
			finally
			{
				//rwl.ReleaseLock();
			}
		}
		#endregion

		#region IServerPool Members


		IMemcachedNode IServerPool.Locate(string key)
		{
			return this.nodeLocator.Locate(key);
		}

		#endregion
	}
}

#region [ License information          ]
/* ************************************************************
 * 
 *    Copyright (c) 2010 Attila Kiskó, enyim.com
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
