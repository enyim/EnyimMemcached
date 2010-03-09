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
		List<MemcachedNode> deadServers = new List<MemcachedNode>();
		// holds all of the currently working servers
		List<MemcachedNode> workingServers = new List<MemcachedNode>();

		private ReadOnlyCollection<MemcachedNode> publicWorkingServers;

		// used to synchronize read/write accesses on the server lists
		private ReaderWriterLock serverAccessLock = new ReaderWriterLock();

		private Timer isAliveTimer;
		private IMemcachedClientConfiguration configuration;
		private IMemcachedKeyTransformer keyTransformer;
		private IMemcachedNodeLocator nodeLocator;
		private ITranscoder transcoder;

		public IEnumerable<MemcachedNode> GetServers()
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
			Type t = this.configuration.KeyTransformer;
			this.keyTransformer = (t == null) ? new DefaultKeyTransformer() : (IMemcachedKeyTransformer)Enyim.Reflection.FastActivator.CreateInstance(t);

			// create the item transcoder instance
			t = this.configuration.Transcoder;
			this.transcoder = (t == null) ? new DefaultTranscoder() : (ITranscoder)Enyim.Reflection.FastActivator.CreateInstance(t);
		}

		//public event Action<PooledSocket> SocketConnected;

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
				Type ltype = this.configuration.NodeLocator;

				IMemcachedNodeLocator l = ltype == null ? new DefaultNodeLocator() : (IMemcachedNodeLocator)Enyim.Reflection.FastActivator.CreateInstance(ltype);
				l.Initialize(this.workingServers);

				this.nodeLocator = l;

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

				List<MemcachedNode> resurrectList = this.deadServers.FindAll(delegate(MemcachedNode node) { return node.Ping(); });

				if (resurrectList.Count > 0)
				{
					this.serverAccessLock.UpgradeToWriterLock(Timeout.Infinite);

					resurrectList.ForEach(delegate(MemcachedNode node)
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
		///  - moves hte node to the  "dead list"
		///  - recreates the locator based on the new list of still functioning servers
		/// </summary>
		/// <param name="node"></param>
		private void MarkAsDead(MemcachedNode node)
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
		private MemcachedNode LocateNode(string itemKey)
		{
			this.serverAccessLock.AcquireReaderLock(Timeout.Infinite);

			try
			{
				MemcachedNode node = this.NodeLocator.Locate(itemKey);
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

			MemcachedNode server = this.LocateNode(itemKey);

			if (server == null)
				return null;

			return server.Acquire();
		}

		public ReadOnlyCollection<MemcachedNode> WorkingServers
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
							this.publicWorkingServers = new ReadOnlyCollection<MemcachedNode>(this.workingServers.ToArray());
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

		public IDictionary<MemcachedNode, IList<string>> SplitKeys(IEnumerable<string> keys)
		{
			Dictionary<MemcachedNode, IList<string>> keysByNode = new Dictionary<MemcachedNode, IList<string>>(MemcachedNode.Comparer.Instance);

			IList<string> nodeKeys;
			MemcachedNode node;

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

				Action<MemcachedNode> cleanupNode = node =>
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

		IEnumerable<MemcachedNode> IServerPool.GetServers()
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
 * Copyright (c) Attila Kiskó, enyim.com
 *
 * This source code is subject to terms and conditions of 
 * Microsoft Permissive License (Ms-PL).
 * 
 * A copy of the license can be found in the License.html
 * file at the root of this distribution. If you can not 
 * locate the License, please send an email to a@enyim.com
 * 
 * By using this source code in any fashion, you are 
 * agreeing to be bound by the terms of the Microsoft 
 * Permissive License.
 *
 * You must not remove this notice, or any other, from this
 * software.
 *
 * ************************************************************/
#endregion