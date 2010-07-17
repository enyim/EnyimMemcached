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
		// holds all dead servers which will be periodically rechecked and put back into the working servers if found alive
		List<IMemcachedNode> deadServers = new List<IMemcachedNode>();
		// holds all of the currently working servers
		List<IMemcachedNode> workingServers = new List<IMemcachedNode>();

		private ReadOnlyCollection<IMemcachedNode> publicWorkingServers;

		// used to synchronize read/write accesses on the server lists
		private ReaderWriterLock serverAccessLock = new ReaderWriterLock();

		private Timer isAliveTimer;
		private IMemcachedClientConfiguration configuration;
		private IMemcachedKeyTransformer keyTransformer;
		private IMemcachedNodeLocator nodeLocator;
		private ITranscoder transcoder;

		public IEnumerable<IMemcachedNode> GetServers()
		{
			return this.WorkingServers;
		}

		public DefaultServerPool(IMemcachedClientConfiguration configuration)
		{
			if (configuration == null)
				throw new ArgumentNullException("configuration", "Invalid or missing pool configuration. Check if the enyim.com/memcached section or your custom section presents in the app/web.config.");

			this.configuration = configuration;
			this.isAliveTimer = new Timer(callback_isAliveTimer, null, (int)this.configuration.SocketPool.DeadTimeout.TotalMilliseconds, (int)this.configuration.SocketPool.DeadTimeout.TotalMilliseconds);

			// create the key transformer instance
			this.keyTransformer = this.configuration.CreateKeyTransformer() ?? new DefaultKeyTransformer();

			// create the item transcoder instance
			this.transcoder = this.configuration.CreateTranscoder() ?? new DefaultTranscoder();
		}

		/// <summary>
		/// This will start the pool: initializes the nodelocator, warms up the socket pools, etc.
		/// </summary>
		public void Start()
		{
			// initialize the server list
			foreach (IPEndPoint ip in this.configuration.Servers)
			{
				MemcachedNode node = new MemcachedNode(ip, this.configuration.SocketPool, this.Authenticator);

				this.workingServers.Add(node);
			}

			// initializes the locator
			this.RebuildIndexes();
		}

		private void RebuildIndexes()
		{
			this.serverAccessLock.UpgradeToWriterLock(Timeout.Infinite);

			try
			{
				var newLocator = this.configuration.CreateNodeLocator();
				newLocator.Initialize(this.workingServers);

				Interlocked.Exchange(ref this.nodeLocator, newLocator);

				this.publicWorkingServers = null;
			}
			finally
			{
				this.serverAccessLock.ReleaseLock();
			}
		}

		/// <summary>
		/// Checks if a dead node is working again.
		/// </summary>
		/// <param name="state"></param>
		private void callback_isAliveTimer(object state)
		{
			this.serverAccessLock.AcquireReaderLock(Timeout.Infinite);

			try
			{
				if (this.deadServers.Count == 0)
					return;

				List<IMemcachedNode> resurrectList = this.deadServers.FindAll(delegate(IMemcachedNode node) { return node.Ping(); });

				if (resurrectList.Count > 0)
				{
					this.serverAccessLock.UpgradeToWriterLock(Timeout.Infinite);

					resurrectList.ForEach(delegate(IMemcachedNode node)
					{
						// maybe it got removed while we were waiting for the writer lock upgrade?
						if (this.deadServers.Remove(node))
							this.workingServers.Add(node);
					});

					this.RebuildIndexes();
				}
			}
			finally
			{
				this.serverAccessLock.ReleaseLock();
			}
		}

		/// <summary>
		/// Marks a node as dead (unusable)
		///  - moves the node to the  "dead list"
		///  - recreates the locator based on the new list of still functioning servers
		/// </summary>
		/// <param name="node"></param>
		private void MarkAsDead(IMemcachedNode node)
		{
			this.serverAccessLock.UpgradeToWriterLock(Timeout.Infinite);

			try
			{
				// server gained AoeREZ while AFK?
				if (!node.IsAlive)
				{
					this.workingServers.Remove(node);
					this.deadServers.Add(node);

					this.RebuildIndexes();
				}
			}
			finally
			{
				this.serverAccessLock.ReleaseLock();
			}
		}

		/// <summary>
		/// Returns the <see cref="t:IKeyTransformer"/> instance used by the pool
		/// </summary>
		public IMemcachedKeyTransformer KeyTransformer
		{
			get { return this.keyTransformer; }
		}

		public IMemcachedNodeLocator NodeLocator
		{
			get { return this.nodeLocator; }
		}

		public ITranscoder Transcoder
		{
			get { return this.transcoder; }
		}

		/// <summary>
		/// Finds the <see cref="T:MemcachedNode"/> which is responsible for the specified item
		/// </summary>
		/// <param name="itemKey"></param>
		/// <returns></returns>
		private IMemcachedNode LocateNode(string itemKey)
		{
			this.serverAccessLock.AcquireReaderLock(Timeout.Infinite);

			try
			{
				IMemcachedNode node = this.NodeLocator.Locate(itemKey);
				if (node == null)
					return null;

				if (node.IsAlive)
					return node;

				this.MarkAsDead(node);

				return this.LocateNode(itemKey);
			}
			finally
			{
				this.serverAccessLock.ReleaseLock();
			}
		}

		public PooledSocket Acquire(string itemKey)
		{
			if (this.serverAccessLock == null)
				throw new ObjectDisposedException("ServerPool");

			IMemcachedNode server = this.LocateNode(itemKey);

			if (server == null)
				return null;

			return server.Acquire();
		}

		public ReadOnlyCollection<IMemcachedNode> WorkingServers
		{
			get
			{
				if (this.publicWorkingServers == null)
				{
					this.serverAccessLock.AcquireReaderLock(Timeout.Infinite);

					try
					{
						if (this.publicWorkingServers == null)
						{
							this.publicWorkingServers = new ReadOnlyCollection<IMemcachedNode>(this.workingServers.ToArray());
						}
					}
					finally
					{
						this.serverAccessLock.ReleaseLock();
					}
				}

				return this.publicWorkingServers;
			}
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

		#region [ IDisposable                  ]
		void IDisposable.Dispose()
		{
			ReaderWriterLock rwl = this.serverAccessLock;

			if (Interlocked.CompareExchange(ref this.serverAccessLock, null, rwl) == null)
				return;

			GC.SuppressFinalize(this);

			try
			{
				rwl.UpgradeToWriterLock(Timeout.Infinite);

				Action<IMemcachedNode> cleanupNode = node =>
				{
					//node.SocketConnected -= this.OnSocketConnected;
					node.Dispose();
				};

				// dispose the nodes (they'll kill conenctions, etc.)
				this.deadServers.ForEach(cleanupNode);
				this.workingServers.ForEach(cleanupNode);

				this.deadServers.Clear();
				this.workingServers.Clear();

				this.nodeLocator = null;

				this.isAliveTimer.Dispose();
				this.isAliveTimer = null;
			}
			finally
			{
				rwl.ReleaseLock();
			}
		}
		#endregion

		#region IServerPool Members

		IMemcachedKeyTransformer IServerPool.KeyTransformer
		{
			get { return this.KeyTransformer; }
		}

		ITranscoder IServerPool.Transcoder
		{
			get { return this.Transcoder; }
		}

		//IAuthenticator IServerPool.Authenticator
		//{
		//    get { return this.authenticator; }
		//}

		PooledSocket IServerPool.Acquire(string key)
		{
			return this.Acquire(key);
		}

		IEnumerable<IMemcachedNode> IServerPool.GetServers()
		{
			return this.GetServers();
		}

		void IServerPool.Start()
		{
			this.Start();
		}

		//event Action<PooledSocket> IServerPool.SocketConnected
		//{
		//    add { this.SocketConnected += value; }
		//    remove { this.SocketConnected -= value; }
		//}

		#endregion

		public IAuthenticator Authenticator { get; set; }
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
