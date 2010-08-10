using System;
using System.Linq;
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

		private IMemcachedNode[] allNodes;

		private IMemcachedClientConfiguration configuration;
		private IOperationFactory factory;
		private IMemcachedNodeLocator nodeLocator;

		public DefaultServerPool(IMemcachedClientConfiguration configuration, IOperationFactory opFactory)
		{
			if (configuration == null) throw new ArgumentNullException("socketConfig");
			if (opFactory == null) throw new ArgumentNullException("opFactory");

			this.factory = opFactory;
		}

		~DefaultServerPool()
		{
			try { ((IDisposable)this).Dispose(); }
			catch { }
		}

		/// <summary>
		/// This will start the pool: initializes the nodelocator, warms up the socket pools, etc.
		/// </summary>
		public void Start()
		{
			// initialize the locator
			var locator = this.configuration.CreateNodeLocator();
			var nodes = from ip in this.configuration.Servers
						select this.CreateNode(ip);

			this.allNodes = nodes.ToArray();

			locator.Initialize(allNodes);
			this.nodeLocator = locator;
		}

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

			// it's the locator's responsibility to return only working nodes
			if (!node.IsAlive)
			{
				if (log.IsWarnEnabled) log.Warn("Locator returned a dead node " + node.EndPoint + " for " + itemKey);
				return null;
			}

			return node;
		}

		protected virtual IMemcachedNode CreateNode(IPEndPoint endpoint)
		{
			return new MemcachedNode(endpoint, this.configuration.SocketPool);
		}

		#region [ IServerPool                  ]

		IMemcachedNode IServerPool.Locate(string key)
		{
			return this.nodeLocator.Locate(key);
		}

		IOperationFactory IServerPool.OperationFactory
		{
			get { return this.factory; }
		}

		IEnumerable<IMemcachedNode> IServerPool.GetServers()
		{
			return this.nodeLocator.GetAll();
		}

		void IServerPool.Start()
		{
			this.Start();
		}

		#endregion
		#region [ IDisposable                  ]

		void IDisposable.Dispose()
		{
			GC.SuppressFinalize(this);

			for (var i = 0; i < this.allNodes.Length; i++)
				try { this.allNodes[i].Dispose(); }
				catch { }

			this.allNodes = null;

			var nd = this.nodeLocator as IDisposable;
			if (nd != null)
				nd.Dispose();

			this.nodeLocator = null;
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
