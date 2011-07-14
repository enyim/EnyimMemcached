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
	internal class MembasePool : IMembaseServerPool
	{
		private static readonly Enyim.Caching.ILog log = Enyim.Caching.LogManager.GetLogger(typeof(MembasePool));

		private IMembaseClientConfiguration configuration;

		private Uri[] poolUrls;
		private BucketConfigListener configListener;

		private InternalState state;

		private string bucketName;
		private string bucketPassword;

		private object DeadSync = new Object();
		private System.Threading.Timer resurrectTimer;
		private bool isTimerActive;
		private long deadTimeoutMsec;

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
			this.configuration = configuration;

			// make null both if we use the default bucket since we do not need to be authenticated
			if (String.IsNullOrEmpty(bucketName) || bucketName == "default")
			{
				this.bucketName = null;
				this.bucketPassword = null;
			}
			else
			{
				this.bucketName = bucketName;
				this.bucketPassword = bucketPassword ?? String.Empty;
			}

			this.deadTimeoutMsec = (long)this.configuration.SocketPool.DeadTimeout.TotalMilliseconds;
		}

		~MembasePool()
		{
			try { ((IDisposable)this).Dispose(); }
			catch { }
		}

		//public VBucketNodeLocator ForwardLocator { get { return this.state.ForwardLocator; } }

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

			// these should be disposed after we've been reinitialized
			var oldNodes = this.state == null ? null : this.state.CurrentNodes;

			// default bucket does not require authentication
			// membase 1.6 tells us if a bucket needs authentication,
			// so let's try to use the config's password
			var password = config.authType == "sasl"
										? config.saslPassword
										: this.bucketPassword;

			var authenticator = this.bucketName == null
						   ? null
						   : new PlainTextAuthenticator(null, this.bucketName, password);

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

			var epa = (from server in vbsm.serverList
					   select ConfigurationHelper.ResolveToEndPoint(server)).ToArray();

			var epaLength = epa.Length;

			for (var i = 0; i < vbsm.vBucketMap.Length; i++)
			{
				var vb = vbsm.vBucketMap[i];
				if (vb == null || vb.Length == 0)
					throw new InvalidOperationException("Server sent an empty vbucket definition at index " + i);
				if (vb[0] >= epaLength || vb[0] < 0)
					throw new InvalidOperationException(String.Format("VBucket line {0} has a master index {1} out of range of the server list ({2})", i, vb[0], epaLength));
			}

			var buckets = vbsm.vBucketMap.Select(a => new VBucket(a[0], a.Skip(1).ToArray())).ToArray();
			var bucketNodeMap = buckets.ToLookup(vb =>
			{
				try
				{
					return epa[vb.Master];
				}
				catch (Exception e)
				{
					log.Error(e);

					throw;
				}
			}
			);
			var vbnl = new VBucketNodeLocator(vbsm.hashAlgorithm, buckets);

			return new InternalState
			{
				CurrentNodes = epa.Select(ip => (IMemcachedNode)new BinaryNode(ip, this.configuration.SocketPool, auth)).ToArray(),
				Locator = vbnl,
				OpFactory = new VBucketAwareOperationFactory(vbnl)
			};
		}

		private InternalState InitBasic(ClusterConfig config, ISaslAuthenticationProvider auth)
		{
			if (log.IsInfoEnabled) log.Info("No vbucket. Server count: " + (config.nodes == null ? 0 : config.nodes.Length));
      List<IMemcachedNode> nodes = new List<IMemcachedNode>();

      if (config != null)
      {
        foreach (ClusterNode v in config.nodes)
        {
          IPAddress address = GetAddress(v.hostname);
          if (address != null)
            nodes.Add(new BinaryNode(new IPEndPoint(address, v.ports.direct), configuration.SocketPool, auth));
        }
      }

			return new InternalState
			{
				CurrentNodes = nodes.ToArray(),
				Locator = configuration.CreateNodeLocator() ?? new KetamaNodeLocator(),
				OpFactory = BasicMembaseOperationFactory.Instance
			};
		}

    private static IPAddress GetAddress(string hostname)
    {
      IPAddress[] items = Dns.GetHostAddresses(hostname);
      if (items.Length > 0)
      {
        if (log.IsInfoEnabled)
        {
          foreach (IPAddress item in items)
          {
            log.Info(string.Format("Found Address {0} found for {1}", item.ToString(), hostname));
          }

          log.Info(string.Format("Using Address {0} for {1}", items.First().ToString(), hostname));
        }
        return items.First();
      }
      else
      {
        log.Info(string.Format("No Address found for {0}", hostname));
        throw new MemcachedClientException(string.Format("Unable to connect to {0} no sutiable addresses were found", hostname));
      }
    }

		void IDisposable.Dispose()
		{
			GC.SuppressFinalize(this);

			if (this.state != null)
				lock (this.DeadSync)
				{
					if (this.state != null)
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
			if (this.state == null) return;

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
				if (this.state == null) return;

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

			this.configListener = new BucketConfigListener(this.poolUrls, this.bucketName, this.bucketPassword)
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

		#endregion
		#region [ InternalState                ]

		private class InternalState
		{
			public static readonly InternalState Empty = new InternalState { CurrentNodes = new IMemcachedNode[0], Locator = new NotFoundLocator() };

			public IMemcachedNodeLocator Locator;
			public VBucketNodeLocator ForwardLocator;
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
