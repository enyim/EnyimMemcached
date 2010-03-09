using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Enyim.Caching.Configuration;
using Enyim.Collections;
using System.Security;

namespace Enyim.Caching.Memcached
{
	/// <summary>
	/// Represents a Memcached node in the pool.
	/// </summary>
	[DebuggerDisplay("{{MemcachedNode [ Address: {EndPoint}, IsAlive = {IsAlive} ]}}")]
	public sealed class MemcachedNode : IDisposable
	{
		private static readonly object SyncRoot = new Object();

		private bool isDisposed;
		private double deadTimeout = 2 * 60;

		private IPEndPoint endPoint;
		private ISocketPoolConfiguration config;
		private InternalPoolImpl internalPoolImpl;
		private IAuthenticator authenticator;

		public MemcachedNode(IPEndPoint endpoint, ISocketPoolConfiguration socketPoolConfig, IAuthenticator authenticator)
		{
			this.endPoint = endpoint;
			this.config = socketPoolConfig;

			this.deadTimeout = socketPoolConfig.DeadTimeout.TotalSeconds;
			if (this.deadTimeout < 0)
				throw new InvalidOperationException("deadTimeout must be >= TimeSpan.Zero");

			this.authenticator = authenticator;
			this.internalPoolImpl = new InternalPoolImpl(this, socketPoolConfig);
		}

		/// <summary>
		/// Gets the <see cref="T:IPEndPoint"/> of this instance
		/// </summary>
		public IPEndPoint EndPoint
		{
			get { return this.endPoint; }
		}

		/// <summary>
		/// <para>Gets a value indicating whether the server is working or not. Returns a <b>cached</b> state.</para>
		/// <para>To get real-time information and update the cached state, use the <see cref="M:Ping"/> method.</para>
		/// </summary>
		/// <remarks>Used by the <see cref="T:ServerPool"/> to quickly check if the server's state is valid.</remarks>
		public bool IsAlive
		{
			get { return this.internalPoolImpl.IsAlive; }
		}

		/// <summary>
		/// Gets a value indicating whether the server is working or not.
		/// 
		/// If the server is not working, and the "being dead" timeout has been expired it will reinitialize itself.
		/// </summary>
		/// <remarks>It's possible that the server is still not up &amp; running so the next call to <see cref="M:Acquire"/> could mark the instance as dead again.</remarks>
		/// <returns></returns>
		internal bool Ping()
		{
			// is the server working?
			if (this.internalPoolImpl.IsAlive)
				return true;

			// deadTimeout was set to 0 which means forever
			if (this.deadTimeout == 0)
				return false;

			TimeSpan diff = DateTime.UtcNow - this.internalPoolImpl.MarkedAsDeadUtc;

			// only do the real check if the configured time interval has passed
			if (diff.TotalSeconds < this.deadTimeout)
				return false;

			// this codepath is (should be) called very rarely
			// if you get here hundreds of times then you have bigger issues
			// and try to make the memcached instaces more stable and/or increase the deadTimeout
			lock (SyncRoot)
			{
				if (this.internalPoolImpl.IsAlive)
					return true;

				// it's easier to create a new pool than reinitializing a dead one
				try { this.internalPoolImpl.Dispose(); }
				catch { }

				Interlocked.Exchange(ref this.internalPoolImpl, new InternalPoolImpl(this, this.config));
			}

			return true;
		}

		/// <summary>
		/// Acquires a new item from the pool
		/// </summary>
		/// <returns>An <see cref="T:PooledSocket"/> instance which is connected to the memcached server, or <value>null</value> if the pool is dead.</returns>
		public PooledSocket Acquire()
		{
			return this.internalPoolImpl.Acquire();
		}

		/// <summary>
		/// Releases all resources allocated by this instance
		/// </summary>
		public void Dispose()
		{
			GC.SuppressFinalize(this);

			// this is not a graceful shutdown
			// if someone uses a pooled item then 99% that an exception will be thrown
			// somewhere. But since the dispose is mostly used when everyone else is finished
			// this should not kill any kittens
			lock (SyncRoot)
			{
				if (this.isDisposed)
					return;

				this.isDisposed = true;

				this.internalPoolImpl.Dispose();
				this.internalPoolImpl = null;
			}
		}

		void IDisposable.Dispose()
		{
			this.Dispose();
		}

		#region [ InternalPoolImpl             ]
		private class InternalPoolImpl : IDisposable
		{
			private static log4net.ILog log = log4net.LogManager.GetLogger(typeof(InternalPoolImpl));

			/// <summary>
			/// A list of already connected but free to use sockets
			/// </summary>
			private InterlockedQueue<PooledSocket> freeItems;

			private bool isDisposed;
			private bool isAlive;
			private DateTime markedAsDeadUtc;

			private int minItems;
			private int maxItems;
			private int workingCount = 0;

			private MemcachedNode ownerNode;
			private IPEndPoint endPoint;
			private ISocketPoolConfiguration config;
			private Semaphore semaphore;

			private bool firstTime = true;
			private object initLock = new Object();

			internal InternalPoolImpl(MemcachedNode ownerNode, ISocketPoolConfiguration config)
			{
				this.ownerNode = ownerNode;
				this.isAlive = true;
				this.endPoint = ownerNode.EndPoint;
				this.config = config;

				this.minItems = config.MinPoolSize;
				this.maxItems = config.MaxPoolSize;

				if (this.minItems < 0)
					throw new InvalidOperationException("minItems must be larger than 0", null);
				if (this.maxItems < this.minItems)
					throw new InvalidOperationException("maxItems must be larger than minItems", null);
				if (this.config.ConnectionTimeout < TimeSpan.Zero)
					throw new InvalidOperationException("connectionTimeout must be >= TimeSpan.Zero", null);

				this.semaphore = new Semaphore(minItems, maxItems, "PoolSemaphore-" + ownerNode.EndPoint);

				this.freeItems = new InterlockedQueue<PooledSocket>();
			}

			private void InitPool()
			{
				try
				{
					if (this.minItems > 0)
					{
						for (int i = 0; i < this.minItems; i++)
						{
							this.freeItems.Enqueue(this.CreateSocket());

							// cannot connect to the server
							if (!this.isAlive)
								break;
						}
					}
				}
				catch (Exception e)
				{
					log.Error("Could not init pool.", e);

					this.MarkAsDead();
				}
			}

			private PooledSocket CreateSocket()
			{
				PooledSocket retval = new PooledSocket(this.endPoint, this.config.ConnectionTimeout, this.config.ReceiveTimeout, this.ReleaseSocket);
				retval.OwnerNode = this.ownerNode;

				if (this.ownerNode.authenticator != null)
					if (!this.ownerNode.authenticator.Authenticate(retval))
						throw new SecurityException("auth failed: " + this.endPoint);

				return retval;
			}

			public bool IsAlive
			{
				get { return this.isAlive; }
			}

			public DateTime MarkedAsDeadUtc
			{
				get { return this.markedAsDeadUtc; }
			}

			/// <summary>
			/// Acquires a new item from the pool
			/// </summary>
			/// <returns>An <see cref="T:PooledSocket"/> instance which is connected to the memcached server, or <value>null</value> if the pool is dead.</returns>
			public PooledSocket Acquire()
			{
				if (log.IsDebugEnabled) log.Debug("Acquiring stream from pool.");

				if (!this.isAlive)
				{
					if (log.IsDebugEnabled) log.Debug("Pool is dead, returning null.");

					return null;
				}

				PooledSocket retval = null;

				if (!this.semaphore.WaitOne(this.config.ConnectionTimeout))
				{
					if (log.IsDebugEnabled) log.Debug("Pool is full, timeouting.");

					// everyone is so busy
					throw new TimeoutException();
				}

				// maye we died while waiting
				if (!this.isAlive)
				{
					if (log.IsDebugEnabled) log.Debug("Pool is dead, returning null.");

					return null;
				}

				// do we have free items?
				if (this.freeItems.Dequeue(out retval))
				{
					#region [ get it from the pool         ]
					try
					{
						retval.Reset();

						if (log.IsDebugEnabled) log.Debug("Socket was reset. " + retval.InstanceId);
#if DEBUG
						Interlocked.Increment(ref this.workingCount);
#endif
						return retval;
					}
					catch (Exception e)
					{
						log.Error("Failed to reset an acquired socket.", e);

						this.MarkAsDead();

						return null;
					}
					#endregion
				}

				// free item pool is empty
				if (log.IsDebugEnabled) log.Debug("Could not get a socket from the pool, Creating a new item.");

				try
				{
					// okay, create the new item
					retval = this.CreateSocket();
#if DEBUG
					Interlocked.Increment(ref this.workingCount);
#endif
				}
				catch (Exception e)
				{
					log.Error("Failed to create socket.", e);
					this.MarkAsDead();

					return null;
				}

				if (log.IsDebugEnabled) log.Debug("Done.");

				return retval;
			}

			private void MarkAsDead()
			{
				if (log.IsWarnEnabled) log.WarnFormat("Marking pool {0} as dead", this.endPoint);

				this.isAlive = false;
				this.markedAsDeadUtc = DateTime.UtcNow;
			}

			/// <summary>
			/// Releases an item back into the pool
			/// </summary>
			/// <param name="socket"></param>
			private void ReleaseSocket(PooledSocket socket)
			{
				if (log.IsDebugEnabled)
				{
					log.Debug("Releasing socket " + socket.InstanceId);
					log.Debug("Are we alive? " + this.isAlive);
				}

				if (this.isAlive)
				{
					// is it still working (i.e. the server is still connected)
					if (socket.IsAlive)
					{
						// mark the item as free
						this.freeItems.Enqueue(socket);
#if DEBUG
						Interlocked.Decrement(ref this.workingCount);
#endif
						// signal the event so if someone is waiting for it can reuse this item
						this.semaphore.Release();
					}
					else
					{
						// kill this item
						socket.Destroy();

						// mark ourselves as not working for a while
						this.MarkAsDead();
					}
				}
				else
				{
					// one of our previous sockets has died, so probably all of them are dead
					// kill the socket thus clearing the pool, and after we become alive
					// we'll fill the pool with working sockets
					socket.Destroy();
				}
			}

			/// <summary>
			/// Releases all resources allocated by this instance
			/// </summary>
			public void Dispose()
			{
				// this is not a graceful shutdown
				// if someone uses a pooled item then 99% that an exception will be thrown
				// somewhere. But since the dispose is mostly used when everyone else is finished
				// this should not kill any kittens
				lock (this)
				{
					this.CheckDisposed();

					this.isAlive = false;
					this.isDisposed = true;

					PooledSocket ps;

					while (this.freeItems.Dequeue(out ps))
					{
						try
						{
							ps.OwnerNode = null;
							ps.Destroy();
						}
						catch { }
					}

					this.ownerNode = null;
					this.semaphore.Close();
					this.semaphore = null;
					this.freeItems = null;
				}
			}

			private void CheckDisposed()
			{
				if (this.isDisposed)
					throw new ObjectDisposedException("pool");
			}

			void IDisposable.Dispose()
			{
				this.Dispose();
			}
		}
		#endregion
		#region [ Comparer                     ]
		internal sealed class Comparer : IEqualityComparer<MemcachedNode>
		{
			public static readonly Comparer Instance = new Comparer();

			bool IEqualityComparer<MemcachedNode>.Equals(MemcachedNode x, MemcachedNode y)
			{
				return x.EndPoint.Equals(y.EndPoint);
			}

			int IEqualityComparer<MemcachedNode>.GetHashCode(MemcachedNode obj)
			{
				return obj.EndPoint.GetHashCode();
			}
		}
		#endregion
		#region [ NodeFactory                  ]
		//internal sealed class NodeFactory
		//{
		//    private Dictionary<string, MemcachedNode> nodeCache = new Dictionary<string, MemcachedNode>(StringComparer.OrdinalIgnoreCase);

		//    internal NodeFactory()
		//    {
		//        AppDomain.CurrentDomain.DomainUnload += DestroyPool;
		//    }

		//    public MemcachedNode Get(IPEndPoint endpoint, IMemcachedClientConfiguration config, IMemcachedAuthenticator authenticator)
		//    {
		//        ISocketPoolConfiguration ispc = config.SocketPool;

		//        string cacheKey = String.Concat(endpoint.ToString(), "-",
		//                                            ispc.ConnectionTimeout.Ticks, "-",
		//                                            ispc.DeadTimeout.Ticks, "-",
		//                                            ispc.MaxPoolSize, "-",
		//                                            ispc.MinPoolSize, "-",
		//                                            ispc.ReceiveTimeout.Ticks);

		//        MemcachedNode node;

		//        if (!nodeCache.TryGetValue(cacheKey, out node))
		//        {
		//            lock (nodeCache)
		//            {
		//                if (!nodeCache.TryGetValue(cacheKey, out node))
		//                {
		//                    node = new MemcachedNode(endpoint, config);

		//                    nodeCache[cacheKey] = node;
		//                }
		//            }
		//        }

		//        return node;
		//    }

		//    private void DestroyPool(object sender, EventArgs e)
		//    {
		//        lock (this.nodeCache)
		//        {
		//            foreach (MemcachedNode node in this.nodeCache.Values)
		//            {
		//                node.Dispose();
		//            }

		//            this.nodeCache.Clear();
		//        }
		//    }
		//}
		#endregion
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