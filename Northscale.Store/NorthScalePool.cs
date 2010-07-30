using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading;
using Enyim.Caching.Memcached;
using NorthScale.Store.Configuration;
using Enyim.Caching.Configuration;

namespace NorthScale.Store
{
	/// <summary>
	/// Socket pool using the NorthScale server's dynamic node list
	/// </summary>
	internal class NorthScalePool : IServerPool
	{
		private INorthScaleClientConfiguration configuration;

		private ITranscoder transcoder;
		private IMemcachedNodeLocator nodeLocator;
		private IMemcachedKeyTransformer keyTransformer;
		private string bucketName;
		private IEnumerable<IMemcachedNode> currentNodes;

		public NorthScalePool(INorthScaleClientConfiguration configuration) : this(configuration, null) { }

		public NorthScalePool(INorthScaleClientConfiguration configuration, string bucket)
		{
			this.configuration = configuration;
			this.bucketName = bucket ?? configuration.Bucket;

			if (String.IsNullOrEmpty(bucketName) || bucketName == "default")
				bucketName = null;

			this.transcoder = configuration.CreateTranscoder() ?? new DefaultTranscoder();
			this.keyTransformer = configuration.CreateKeyTransformer() ?? new DefaultKeyTransformer();
		}

		~NorthScalePool()
		{
			try { ((IDisposable)this).Dispose(); }
			catch { }
		}

		private static object Create(Type type)
		{
			if (type == null) return null;

			return Enyim.Reflection.FastActivator2.Create(type);
		}

		IMemcachedKeyTransformer IServerPool.KeyTransformer
		{
			get { return keyTransformer; }
		}

		ITranscoder IServerPool.Transcoder
		{
			get { return transcoder; }
		}

		IAuthenticator IServerPool.Authenticator { get; set; }

		PooledSocket IServerPool.Acquire(string key)
		{
			var node = this.nodeLocator.Locate(key);

			// we aren't supposed to get dead servers 
			// or, if we get one it should disappear from the list pretty soon
			// so we'll keep this as it is for a while
			// (returning null, so at least the operations will fail silently)
			if (node == null || !node.IsAlive) return null;

			return node.Acquire();
		}

		IEnumerable<IMemcachedNode> IServerPool.GetServers()
		{
			return this.currentNodes;
		}

		private Uri[] poolUrls;
		private BucketConfigListener configListener;

		void IServerPool.Start()
		{
			// get the pool urls
			this.poolUrls = this.configuration.Urls.ToArray();
			if (this.poolUrls.Length == 0)
				throw new InvalidOperationException("At least 1 pool url must be specified.");

			var helper = new ConfigHelper
			{
				Credentials = this.configuration.Credentials,
				Timeout = (int)this.configuration.SocketPool.ConnectionTimeout.TotalMilliseconds
			};

			this.configListener = new BucketConfigListener(this.bucketName, helper, this.poolUrls)
			{
				DeadTimeout = (int)this.configuration.SocketPool.DeadTimeout.TotalMilliseconds
			};

			this.configListener.ClusterConfigChanged += this.InitNodes;

			// start blocks until the first NodeListChanged event is triggered
			this.configListener.Start();
		}

		private void InitNodes(ClusterConfig config)
		{
			// default bucket does not require authentication
			var auth = this.bucketName == null ? null : ((IServerPool)this).Authenticator;

			IEnumerable<IPEndPoint> endpoints;
			IMemcachedNodeLocator locator;

			if (config == null || config.vBucketServerMap == null)
			{
				// no vbucket config, use the node list and the ports
				var portType = this.configuration.Port;

				endpoints = config == null
							? Enumerable.Empty<IPEndPoint>()
							: (from node in config.nodes
							   where node.status == "healthy"
							   select new IPEndPoint(
											IPAddress.Parse(node.hostname),
											(portType == BucketPortType.Proxy
												? node.ports.proxy
												: node.ports.direct)));

				locator = this.configuration.CreateNodeLocator() ?? new KetamaNodeLocator();
			}
			else
			{
				// we have a vbucket config, which has its own server list
				// it's supposed to be the same as the cluster config's list,
				// but the order is significicant (because of the bucket indexes),
				// so we we'll use this for initializing the locator
				var vbsm = config.vBucketServerMap;
				endpoints = from server in vbsm.serverList
							let parts = server.Split(':')
							select new IPEndPoint(IPAddress.Parse(parts[0]), Int32.Parse(parts[1]));

				locator = new VBucketNodeLocator(vbsm.hashAlgorithm, vbsm.vBucketMap.Select(a => new VBucket(a[0], a.Skip(1).ToArray())).ToArray());
			}

			var mcNodes = (from e in endpoints
						   select new MemcachedNode(e, this.configuration.SocketPool, auth)).ToArray();

			locator.Initialize(mcNodes);

			Interlocked.Exchange(ref this.currentNodes, new ReadOnlyCollection<IMemcachedNode>(mcNodes));
			Interlocked.Exchange(ref this.nodeLocator, locator);
		}

		void IDisposable.Dispose()
		{
			if (this.configListener != null)
			{
				this.configListener.Stop();
				this.configListener = null;
			}
		}

		IMemcachedNodeLocator IServerPool.NodeLocator
		{
			get { return this.nodeLocator; }
		}
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
