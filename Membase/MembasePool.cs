using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Enyim.Caching.Configuration;
using Enyim.Caching.Memcached;
using Enyim.Caching.Memcached.Protocol.Binary;
using Membase.Configuration;

namespace Membase
{
	/// <summary>
	/// Socket pool using the Membase server's dynamic node list
	/// </summary>
	public class MembasePool : IMembaseServerPool
	{
		private static readonly Enyim.Caching.ILog log = Enyim.Caching.LogManager.GetLogger(typeof(MembasePool));

		private IMembaseClientConfiguration configuration;

		private Uri[] poolUrls;
		private BucketConfigListener configListener;

		private InternalState state;

		private object DeadSync = new Object();
		private System.Threading.Timer resurrectTimer;
		private bool isTimerActive;
		private long deadTimeoutMsec;
		private event Action<IMemcachedNode> nodeFailed;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Membase.MembasePool" />.
		/// </summary>
		/// <param name="configuration">The configuration to be used.</param>
		public MembasePool(IMembaseClientConfiguration configuration)
		{
			if (configuration == null) throw new ArgumentNullException("configuration");

			this.Initialize(configuration, configuration.Bucket, configuration.BucketPassword);
		}

		/// <summary>Obsolete. Use .ctor(config, bucket, password) to explicitly set the bucket password.</summary>
		[Obsolete("Use .ctor(config, bucket, password) to explicitly set the bucket password.", true)]
		public MembasePool(IMembaseClientConfiguration configuration, string bucket)
		{
			throw new InvalidOperationException("Use .ctor(config, bucket, password) to explicitly set the bucket password.");
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Membase.MembasePool" /> class using the specified configuration,
		/// bucket name and password.
		/// </summary>
		/// <param name="configuration">The configuration to be used.</param>
		/// <param name="bucketName">The name of the bucket to connect to. Overrides the configuration's Bucket property.</param>
		/// <param name="bucketPassword">The password to the bucket. Overrides the configuration's BucketPassword property.</param>
		public MembasePool(IMembaseClientConfiguration configuration, string bucketName, string bucketPassword)
		{
			if (configuration == null) throw new ArgumentNullException("configuration");

			this.Initialize(configuration, bucketName, bucketPassword);
		}

		private void Initialize(IMembaseClientConfiguration configuration, string bucketName, string bucketPassword)
		{
			var roc = new ReadOnlyConfig(configuration);

			// make null both if we use the default bucket since we do not need to be authenticated
			if (String.IsNullOrEmpty(bucketName) || bucketName == "default")
			{
				bucketName = null;
				bucketPassword = null;
			}
			else
			{
				bucketPassword = bucketPassword ?? String.Empty;
			}

			roc.OverrideBucket(bucketName, bucketPassword);

			this.configuration = roc;
			this.deadTimeoutMsec = (long)this.configuration.SocketPool.DeadTimeout.TotalMilliseconds;
		}

		~MembasePool()
		{
			try { ((IDisposable)this).Dispose(); }
			catch { }
		}

		protected IMembaseClientConfiguration Configuration
		{
			get { return this.configuration; }
		}

		private void InitNodes(ClusterConfig config)
		{
			if (log.IsInfoEnabled) log.Info("Received new configuration.");

			// we cannot overwrite the config while the timer is is running
			lock (this.DeadSync)
				this.ReconfigurePool(config);
		}

		private void ReconfigurePool(ClusterConfig config)
		{
			// kill the timer first
			this.isTimerActive = false;
			if (this.resurrectTimer != null)
				this.resurrectTimer.Change(Timeout.Infinite, Timeout.Infinite);

			if (config == null)
			{
				if (log.IsInfoEnabled) log.Info("Config is empty, all nodes are down.");

				Interlocked.Exchange(ref this.state, InternalState.Empty);

				return;
			}

			var currentState = this.state;

			// these should be disposed after we've been reinitialized
			var oldNodes = currentState == null ? null : currentState.CurrentNodes;

			// default bucket does not require authentication
			// membase 1.6 tells us if a bucket needs authentication,
			// so let's try to use the config's password
			var password = config.authType == "sasl"
							? config.saslPassword
							: this.configuration.BucketPassword;

			var authenticator = this.configuration.Bucket == null
								   ? null
								   : new PlainTextAuthenticator(null, this.configuration.Bucket, password);

			try
			{
				var state = (config == null || config.vBucketServerMap == null)
								? this.InitBasic(config, authenticator)
								: this.InitVBucket(config, authenticator);

				var nodes = state.CurrentNodes;

				state.Locator.Initialize(nodes);

				// we need to subscribe the failed event, 
				// so we can periodically check the dead 
				// nodes, since we do not get a config 
				// update every time a node dies
				for (var i = 0; i < nodes.Length; i++) nodes[i].Failed += this.NodeFail;

				Interlocked.Exchange(ref this.state, state);
			}
			catch (Exception e)
			{
				log.Error("Failed to initialize the pool.", e);

				Interlocked.Exchange(ref this.state, InternalState.Empty);
			}

			// kill the old nodes
			if (oldNodes != null)
				for (var i = 0; i < oldNodes.Length; i++)
					try
					{
						oldNodes[i].Failed -= this.NodeFail;
						oldNodes[i].Dispose();
					}
					catch { }
		}

		private InternalState InitVBucket(ClusterConfig config, ISaslAuthenticationProvider auth)
		{
			// we have a vbucket config, which has its own server list
			// it's supposed to be the same as the cluster config's list,
			// but the order is significicant (because of the bucket indexes),
			// so we we'll use this for initializing the locator
			var vbsm = config.vBucketServerMap;

			if (log.IsInfoEnabled) log.Info("Has vbucket. Server count: " + (vbsm.serverList == null ? 0 : vbsm.serverList.Length));

			// parse the ip addresses of the servers in the vbucket map
			// make sure we have a propert vbucket map
			ValidateVBucketMap(vbsm, vbsm.serverList.Length);

			// create vbuckets from the int[][] arrays
			var buckets = vbsm.vBucketMap.Select(a => new VBucket(a[0], a.Skip(1).ToArray())).ToArray();

			var locator = new VBucketNodeLocator(vbsm.hashAlgorithm, buckets);

			// create a (host=>node) lookup from the node info objects, 
			// so we can pass the extra config data to the factory method
			// (the vbucket map only contains 'host:port' strings)
			// this expects that all nodes listed in the vbucket map are listed in the config.nodes member as well
			var realNodes = config.nodes.ToDictionary(node => node.HostName + ":" + node.Port);
			var nodes = new List<IMemcachedNode>();

			foreach (var hostSpec in vbsm.serverList)
			{
				ClusterNode node;

				if (!realNodes.TryGetValue(hostSpec, out node))
					throw new InvalidOperationException(String.Format("VBucket map contains a node {0} whihc was not found in the cluster info's node list.", hostSpec));

				var ip = GetFirstAddress(node.HostName);
				var endpoint = new IPEndPoint(ip, node.Port);

				nodes.Add(this.CreateNode(endpoint, auth, node.ConfigurationData));
			}

			return new InternalState
			{
				CurrentNodes = nodes.ToArray(),
				Locator = locator,
				OpFactory = new VBucketAwareOperationFactory(locator),
				IsVbucket = true
			};
		}

		private static void ValidateVBucketMap(VBucketConfig vbsm, int knownNodeCount)
		{
			for (var i = 0; i < vbsm.vBucketMap.Length; i++)
			{
				var map = vbsm.vBucketMap[i];
				if (map == null || map.Length == 0)
					throw new InvalidOperationException("Server sent an empty vbucket definition at index " + i);
				if (map[0] >= knownNodeCount || map[0] < 0)
					throw new InvalidOperationException(String.Format("VBucket line {0} has a master index {1} out of range of the server list ({2})", i, map[0], knownNodeCount));
			}
		}

		private InternalState InitBasic(ClusterConfig config, ISaslAuthenticationProvider auth)
		{
			if (log.IsInfoEnabled) log.Info("No vbucket. Server count: " + (config.nodes == null ? 0 : config.nodes.Length));

			// the cluster can return host names in the server list, so
			// we ha ve to make sure they are converted to IP addresses
			var nodes = config == null
						? Enumerable.Empty<IMemcachedNode>()
							: (from node in config.nodes
							   let ip = new IPEndPoint(GetFirstAddress(node.HostName), node.Port)
							   where node.Status == "healthy"
							   select CreateNode(ip, auth, node.ConfigurationData));

			return new InternalState
			{
				CurrentNodes = nodes.ToArray(),
				Locator = configuration.CreateNodeLocator() ?? new KetamaNodeLocator(),
				OpFactory = BasicMembaseOperationFactory.Instance
			};
		}

		private static IPAddress GetFirstAddress(string hostname)
		{
			var items = Dns.GetHostAddresses(hostname);

			// if either the dns is not set up properly
			// or the host is mapped only to an IPv6 address
			// but the client has no IPv6 stack
			// then GetHostAddresses will not return anything
			if (items.Length > 0)
			{
				if (log.IsDebugEnabled)
					foreach (IPAddress item in items)
						log.DebugFormat("Found address {0} for {1}", item, hostname);

				var retval = items[0];

				if (log.IsDebugEnabled)
					log.DebugFormat("Using address {0} for {1}", retval, hostname);

				return retval;
			}

			if (log.IsErrorEnabled)
				log.Error("Could not resolve " + hostname);

			throw new MemcachedClientException("Could not resolve " + hostname);
		}

		protected virtual IMemcachedNode CreateNode(IPEndPoint endpoint, ISaslAuthenticationProvider auth, Dictionary<string, object> nodeInfo)
		{
			return new BinaryNode(endpoint, this.configuration.SocketPool, auth);
		}

		void IDisposable.Dispose()
		{
			GC.SuppressFinalize(this);

			if (this.state != null && this.state != InternalState.Empty)
				lock (this.DeadSync)
				{
					if (this.state != null && this.state != InternalState.Empty)
					{
						var currentNodes = this.state.CurrentNodes;
						this.state = null;

						this.configListener.Stop();
						this.configListener = null;

						if (this.resurrectTimer != null)
							using (this.resurrectTimer)
								this.resurrectTimer.Change(Timeout.Infinite, Timeout.Infinite);

						this.resurrectTimer = null;

						// close the pools
						if (currentNodes != null)
							for (var i = 0; i < currentNodes.Length; i++)
								currentNodes[i].Dispose();
					}
				}
		}

		private void rezCallback(object o)
		{
			if (this.state == null || this.state == InternalState.Empty) return;

			var isDebug = log.IsDebugEnabled;

			if (isDebug) log.Debug("Checking the dead servers.");

			// how this works:
			// 1. timer is created but suspended
			// 2. Locate encounters a dead server, so it starts the timer which will trigger after deadTimeout has elapsed
			// 3. if another server goes down before the timer is triggered, nothing happens in Locate (isRunning == true).
			//		however that server will be inspected sooner than Dead Timeout.
			//		   S1 died   S2 died    dead timeout
			//		|----*--------*------------*-
			//           |                     |
			//          timer start           both servers are checked here
			// 4. we iterate all the servers and record it in another list
			// 5. if we found a dead server whihc responds to Ping(), the locator will be reinitialized
			// 6. if at least one server is still down (Ping() == false), we restart the timer
			// 7. if all servers are up, we set isRunning to false, so the timer is suspended
			// 8. GOTO 2
			lock (this.DeadSync)
			{
				if (this.state == null || this.state == InternalState.Empty) return;

				var currentState = this.state;
				var nodes = currentState.CurrentNodes;
				var aliveList = new List<IMemcachedNode>(nodes.Length);
				var deadCount = 0;
				var changed = false;

				#region [ Ping the servers             ]
				for (var i = 0; i < nodes.Length; i++)
				{
					var n = nodes[i];
					if (n.IsAlive)
					{
						if (isDebug) log.DebugFormat("Alive: {0}", n.EndPoint);
					}
					else
					{
						if (isDebug) log.DebugFormat("Dead: {0}", n.EndPoint);

						if (n.Ping())
						{
							changed = true;
							if (isDebug) log.Debug("Ping ok.");
						}
						else
						{
							if (isDebug) log.Debug("Still dead.");

							deadCount++;
						}
					}
				}
				#endregion

				if (changed && !currentState.IsVbucket)
				{
					if (isDebug) log.Debug("We have a standard config, so we'll recreate the node locator.");

					ReinitializeLocator(currentState);
				}

				// stop or restart the timer
				if (deadCount == 0)
				{
					if (isDebug) log.Debug("deadCount == 0, stopping the timer.");

					this.isTimerActive = false;
				}
				else
				{
					if (isDebug) log.DebugFormat("deadCount == {0}, starting the timer.", deadCount);

					this.resurrectTimer.Change(this.deadTimeoutMsec, Timeout.Infinite);
				}
			}
		}

		private void NodeFail(IMemcachedNode node)
		{
			var isDebug = log.IsDebugEnabled;
			if (isDebug) log.DebugFormat("Node {0} is dead.", node.EndPoint);

			// block the rest api listener until we're finished here
			lock (this.DeadSync)
			{
				var currentState = this.state;

				// the pool has been already reinitialized by the time the node
				// reported its failure, thus it has no connection to the current state
				if (currentState == null || currentState == InternalState.Empty) return;

				var fail = this.nodeFailed;
				if (fail != null)
					fail(node);

				// we don't know who to reconfigure the pool when vbucket is
				// enabled, so operations targeting the dead servers will fail.
				// when we have a normal config we just reconfigure the locator,
				// so the items will be rehashed to the working servers
				if (!currentState.IsVbucket)
				{
					if (isDebug) log.Debug("We have a standard config, so we'll recreate the node locator.");

					ReinitializeLocator(currentState);
				}

				// the timer is stopped until we encounter the first dead server
				// when we have one, we trigger it and it will run after DeadTimeout has elapsed
				if (!this.isTimerActive)
				{
					if (isDebug) log.Debug("Starting the recovery timer.");

					if (this.resurrectTimer == null)
						this.resurrectTimer = new Timer(this.rezCallback, null, this.deadTimeoutMsec, Timeout.Infinite);
					else
						this.resurrectTimer.Change(this.deadTimeoutMsec, Timeout.Infinite);

					this.isTimerActive = true;

					if (isDebug) log.Debug("Timer started.");
				}
			}

			if (isDebug) log.Debug("Fail handler is finished.");
		}

		private void ReinitializeLocator(InternalState previousState)
		{
			var newState = new InternalState
			{
				CurrentNodes = previousState.CurrentNodes,
				IsVbucket = false,
				OpFactory = previousState.OpFactory,
				Locator = this.configuration.CreateNodeLocator()
			};

			if (log.IsDebugEnabled) log.Debug("Initializing the locator with the list of working nodes.");

			newState.Locator.Initialize(newState.CurrentNodes.Where(n => n.IsAlive).ToArray());

			Interlocked.Exchange(ref this.state, newState);

			if (log.IsDebugEnabled) log.Debug("Replaced the internal state.");
		}

		#region [ IServerPool                  ]

		IMemcachedNode IServerPool.Locate(string key)
		{
			return this.state.Locator.Locate(key);
		}

		IOperationFactory IServerPool.OperationFactory
		{
			get { return this.state.OpFactory; }
		}

		IMembaseOperationFactory IMembaseServerPool.OperationFactory
		{
			get { return this.state.OpFactory; }
		}

		IEnumerable<IMemcachedNode> IServerPool.GetWorkingNodes()
		{
			return this.state.Locator.GetWorkingNodes();
		}

		void IServerPool.Start()
		{
			// get the pool urls
			this.poolUrls = this.configuration.Urls.ToArray();
			if (this.poolUrls.Length == 0)
				throw new InvalidOperationException("At least 1 pool url must be specified.");

			this.configListener = new BucketConfigListener(this.poolUrls, this.configuration.Bucket, this.configuration.BucketPassword)
			{
				Timeout = (int)this.configuration.SocketPool.ConnectionTimeout.TotalMilliseconds,
				DeadTimeout = (int)this.configuration.SocketPool.DeadTimeout.TotalMilliseconds,
				RetryCount = this.configuration.RetryCount,
				RetryTimeout = this.configuration.RetryTimeout
			};

			this.configListener.ClusterConfigChanged += this.InitNodes;

			// start blocks until the first NodeListChanged event is triggered
			this.configListener.Start();
		}

		event Action<IMemcachedNode> IServerPool.NodeFailed
		{
			add { this.nodeFailed += value; }
			remove { this.nodeFailed -= value; }
		}

		#endregion
		#region [ InternalState                ]

		private class InternalState
		{
			public static readonly InternalState Empty = new InternalState { CurrentNodes = new IMemcachedNode[0], Locator = new NotFoundLocator() };

			public IMemcachedNodeLocator Locator;
			//public VBucketNodeLocator ForwardLocator;
			public IMembaseOperationFactory OpFactory;
			public IMemcachedNode[] CurrentNodes;

			// if this is false, it's safe to reinitialize/recreate the locator when a server goes offline
			public bool IsVbucket;
		}


		#endregion
		#region [ NotFoundLocator              ]

		private class NotFoundLocator : IMemcachedNodeLocator
		{
			public static readonly IMemcachedNodeLocator Instance = new NotFoundLocator();

			void IMemcachedNodeLocator.Initialize(IList<IMemcachedNode> nodes)
			{
			}

			IMemcachedNode IMemcachedNodeLocator.Locate(string key)
			{
				return null;
			}

			IEnumerable<IMemcachedNode> IMemcachedNodeLocator.GetWorkingNodes()
			{
				return Enumerable.Empty<IMemcachedNode>();
			}
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
