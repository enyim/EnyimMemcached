using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading;
using Enyim.Caching.Memcached;
using NorthScale.Store.Configuration;

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

			this.transcoder = (ITranscoder)Create(configuration.Transcoder) ?? new DefaultTranscoder();
			this.keyTransformer = (IMemcachedKeyTransformer)Create(configuration.KeyTransformer) ?? new DefaultKeyTransformer();
		}

		private static object Create(Type type)
		{
			if (type == null) return null;

			return Enyim.Reflection.FastActivator.CreateInstance(type);
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
				throw new InvalidOperationException("at least 1 pool url must be specified.");

			var cm = new ConfigHelper();
			cm.Credentials = this.configuration.Credentials;

			// get the nodes of the specified buctek using the pool urls 
			var nodes = cm.GetWorkingNodes(this.poolUrls, this.bucketName ?? "default");
			if (nodes == null || nodes.Length == 0)
				throw new InvalidOperationException("none of the pool urls are working.");

			// initialize the locator and create the MemcachedNodes
			this.InitNodes(nodes);

			// discover the streamingUri of the bucket
			var streamingUris = cm.GetBucketStreamingUris(this.poolUrls, this.bucketName ?? "default");
			if (streamingUris == null)
				throw new InvalidOperationException("none of the pool urls are working.");

			// create a config listener
			this.configListener = new BucketConfigListener(streamingUris, nodes);
			this.configListener.Credentials = this.configuration.Credentials;

			// recreate the node lcoator when the config changes
			this.configListener.NodeListChanged += InitNodes;

			// start the istener by 500 msec delay to let the system settle before stressing it with more work
			this.configListener.Start(500);
		}

		private void InitNodes(IEnumerable<BucketNode> nodes)
		{
			// default bucket does not require authentication
			var auth = this.bucketName == null ? null : ((IServerPool)this).Authenticator;

			var mcNodes = nodes.Select(b => new MemcachedNode(
												new IPEndPoint(IPAddress.Parse(b.hostname), b.ports.direct),
												this.configuration.SocketPool,
												auth)).ToArray();

			var locator = (IMemcachedNodeLocator)Create(this.configuration.NodeLocator) ?? new KetamaNodeLocator();
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
