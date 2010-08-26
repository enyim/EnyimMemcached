using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading;
using Enyim.Caching.Memcached;
using NorthScale.Store.Configuration;
using Enyim.Caching.Configuration;
using Enyim.Caching.Memcached.Protocol.Binary;

namespace NorthScale.Store
{
	/// <summary>
	/// Socket pool using the NorthScale server's dynamic node list
	/// </summary>
	internal class NorthScalePool : IServerPool
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(NorthScalePool));

		private INorthScaleClientConfiguration configuration;

		private Uri[] poolUrls;
		private BucketConfigListener configListener;

		private IMemcachedNodeLocator nodeLocator;
		private IOperationFactory operationFactory;

		private string bucketName;
		private IMemcachedNode[] currentNodes;

		public NorthScalePool(INorthScaleClientConfiguration configuration) : this(configuration, null) { }

		public NorthScalePool(INorthScaleClientConfiguration configuration, string bucket)
		{
			this.configuration = configuration;
			this.bucketName = bucket ?? configuration.Bucket;

			if (String.IsNullOrEmpty(bucketName) || bucketName == "default")
				bucketName = null;
		}

		~NorthScalePool()
		{
			try { ((IDisposable)this).Dispose(); }
			catch { }
		}

		void IServerPool.Start()
		{
			// get the pool urls
			this.poolUrls = this.configuration.Urls.ToArray();
			if (this.poolUrls.Length == 0)
				throw new InvalidOperationException("At least 1 pool url must be specified.");

			this.configListener = new BucketConfigListener(this.poolUrls, this.bucketName, this.configuration.Credentials)
			{
				Timeout = (int)this.configuration.SocketPool.ConnectionTimeout.TotalMilliseconds,
				DeadTimeout = (int)this.configuration.SocketPool.DeadTimeout.TotalMilliseconds
			};

			this.configListener.ClusterConfigChanged += this.InitNodes;

			// start blocks until the first NodeListChanged event is triggered
			this.configListener.Start();
		}

		private void InitNodes(ClusterConfig config)
		{
			if (log.IsInfoEnabled) log.Info("Received new configuration.");

			// these should be disposed after we've been reinitialized
			var oldNodes = this.currentNodes;

			// default bucket does not require authentication
			var auth = this.bucketName == null
						? null
						: new PlainTextAuthenticator(null, this.bucketName, this.bucketName);

			IEnumerable<IMemcachedNode> nodes;
			IMemcachedNodeLocator locator;

			if (config == null || config.vBucketServerMap == null)
			{
				// no vbucket config, use the node list and the ports
				var portType = this.configuration.Port;

				nodes = config == null
						? Enumerable.Empty<IMemcachedNode>()
							: (from node in config.nodes
							   let ip = new IPEndPoint(IPAddress.Parse(node.hostname),
														(portType == BucketPortType.Proxy
															? node.ports.proxy
															: node.ports.direct))
							   where node.status == "healthy"
							   select (IMemcachedNode)(new BinaryNode(ip, this.configuration.SocketPool, auth)));

				locator = this.configuration.CreateNodeLocator() ?? new KetamaNodeLocator();

				this.operationFactory = new Enyim.Caching.Memcached.Protocol.Binary.BinaryOperationFactory();
			}
			else
			{
				// we have a vbucket config, which has its own server list
				// it's supposed to be the same as the cluster config's list,
				// but the order is significicant (because of the bucket indexes),
				// so we we'll use this for initializing the locator
				var vbsm = config.vBucketServerMap;
				var endpoints = (from server in vbsm.serverList
								 let parts = server.Split(':')
								 select new IPEndPoint(IPAddress.Parse(parts[0]), Int32.Parse(parts[1])));

				var epa = endpoints.ToArray();
				var buckets = vbsm.vBucketMap.Select(a => new VBucket(a[0], a.Skip(1).ToArray())).ToArray();
				var bucketNodeMap = buckets.ToLookup(vb => epa[vb.Master]);
				var vbnl = new VBucketNodeLocator(vbsm.hashAlgorithm, buckets);

				// assign the first bucket index to the servers until 
				// we can solve how to assign the appropriate vbucket index to each command buffer
				nodes = from ip in endpoints
						let bucket = Array.IndexOf(buckets, bucketNodeMap[ip].FirstOrDefault())
						select (IMemcachedNode)(new BinaryNode(ip, this.configuration.SocketPool, auth));

				locator = vbnl;

				this.operationFactory = new VBucketAwareOperationFactory(vbnl);
			}

			var mcNodes = nodes.ToArray();
			locator.Initialize(mcNodes);

			Interlocked.Exchange(ref this.currentNodes, mcNodes);
			Interlocked.Exchange(ref this.nodeLocator, locator);

			if (oldNodes != null)
				for (var i = 0; i < oldNodes.Length; i++)
					oldNodes[i].Dispose();
		}

		void IDisposable.Dispose()
		{
			if (this.configListener != null)
			{
				this.configListener.Stop();

				this.configListener = null;

				var currentNodes = this.currentNodes;

				// close the pools
				if (currentNodes != null)
				{
					for (var i = 0; i < currentNodes.Length; i++)
						currentNodes[i].Dispose();

					this.currentNodes = null;
				}
			}
		}

		IMemcachedNode IServerPool.Locate(string key)
		{
			return this.nodeLocator.Locate(key);
		}

		IOperationFactory IServerPool.OperationFactory
		{
			get { return this.operationFactory; }
		}

		IEnumerable<IMemcachedNode> IServerPool.GetWorkingNodes()
		{
			return this.nodeLocator.GetWorkingNodes();
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
