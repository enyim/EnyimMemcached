using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Threading;
using Enyim.Caching.Configuration;

namespace Enyim.Caching.Memcached
{
	public class DefaultServerPool : IServerPool, IDisposable
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(DefaultServerPool));

		private IMemcachedNode[] allNodes;

		private IMemcachedClientConfiguration configuration;
		private IOperationFactory factory;
		private IMemcachedNodeLocator nodeLocator;
		private System.Threading.Timer resurrectTimer;
		private bool isTimerActive;

		public DefaultServerPool(IMemcachedClientConfiguration configuration, IOperationFactory opFactory)
		{
			if (configuration == null) throw new ArgumentNullException("socketConfig");
			if (opFactory == null) throw new ArgumentNullException("opFactory");

			this.configuration = configuration;
			this.factory = opFactory;

			// timer starts as disabled
			this.resurrectTimer = new Timer(this.rezCallback, null, Timeout.Infinite, Timeout.Infinite);
		}

		private void rezCallback(object state)
		{
			if (log.IsDebugEnabled) log.Debug("Checking the dead servers.");

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
			lock (this.resurrectTimer)
			{
				var aliveList = new List<IMemcachedNode>(this.allNodes.Length);
				var mustInit = false;
				var deadCount = 0;

				for (var i = 0; i < this.allNodes.Length; i++)
				{
					var n = this.allNodes[i];
					if (n.IsAlive)
					{
						if (log.IsDebugEnabled) log.DebugFormat("Alive: {0}", n.EndPoint);

						aliveList.Add(n);
					}
					else
					{
						if (log.IsDebugEnabled) log.DebugFormat("Dead: {0}", n.EndPoint);

						if (n.Ping())
						{
							if (log.IsDebugEnabled) log.Debug("Ping ok.");

							mustInit = true;
							aliveList.Add(n);
						}
						else
						{
							if (log.IsDebugEnabled) log.Debug("Still dead.");

							deadCount++;
						}
					}
				}

				// reinit the locator
				if (mustInit)
				{
					if (log.IsDebugEnabled) log.Debug("Reinitializing the locator.");

					this.nodeLocator.Initialize(aliveList);
				}

				// stop or restart the timer
				if (deadCount == 0)
				{
					if (log.IsDebugEnabled) log.Debug("deadCount == 0, stopping the timer.");

					this.isTimerActive = false;
				}
				else
				{
					if (log.IsDebugEnabled) log.DebugFormat("deadCount == {0}, starting the timer.", deadCount);

					this.resurrectTimer.Change((long)this.configuration.SocketPool.DeadTimeout.TotalMilliseconds, Timeout.Infinite);
				}
			}
		}

		~DefaultServerPool()
		{
			try { ((IDisposable)this).Dispose(); }
			catch { }
		}

		protected virtual IMemcachedNode CreateNode(IPEndPoint endpoint)
		{
			return new MemcachedNode(endpoint, this.configuration.SocketPool);
		}

		private void NodeFail(IMemcachedNode node)
		{
			if (log.IsDebugEnabled) log.DebugFormat("Node {0} is dead, starting the timer.", node.EndPoint);

			// the timer is stopped until we encounter the first dead server
			// when we have one, we trigger it and it will run after DeadTimeout has elapsed
			if (!this.isTimerActive)
				lock (this.resurrectTimer)
					if (!this.isTimerActive)
					{
						this.isTimerActive = true;
						this.resurrectTimer.Change((long)configuration.SocketPool.DeadTimeout.TotalMilliseconds, Timeout.Infinite);

						if (log.IsDebugEnabled) log.Debug("Timer started.");
					}
		}

		#region [ IServerPool                  ]

		IMemcachedNode IServerPool.Locate(string key)
		{
			var node = this.nodeLocator.Locate(key);

			return node;
		}

		IOperationFactory IServerPool.OperationFactory
		{
			get { return this.factory; }
		}

		IEnumerable<IMemcachedNode> IServerPool.GetWorkingNodes()
		{
			return this.nodeLocator.GetWorkingNodes();
		}

		void IServerPool.Start()
		{
			this.allNodes = this.configuration.Servers.
								Select(ip =>
								{
									var node = this.CreateNode(ip);
									node.Failed += this.NodeFail;

									return node;
								}).
								ToArray();

			// initialize the locator
			var locator = this.configuration.CreateNodeLocator();
			locator.Initialize(allNodes);

			this.nodeLocator = locator;
		}

		#endregion
		#region [ IDisposable                  ]

		void IDisposable.Dispose()
		{
			if (this.allNodes != null)
			{
				GC.SuppressFinalize(this);

				for (var i = 0; i < this.allNodes.Length; i++)
					try { this.allNodes[i].Dispose(); }
					catch { }

				this.allNodes = null;

				var nd = this.nodeLocator as IDisposable;
				if (nd != null)
					try { nd.Dispose(); }
					catch { }

				this.nodeLocator = null;

				using (this.resurrectTimer)
					this.resurrectTimer.Change(Timeout.Infinite, Timeout.Infinite);

				this.resurrectTimer = null;
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
