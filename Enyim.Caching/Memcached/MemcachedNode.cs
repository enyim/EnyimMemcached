using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Enyim.Caching.Configuration;
using Enyim.Collections;
using System.Security;
using Enyim.Caching.Memcached.Protocol.Binary;

namespace Enyim.Caching.Memcached
{
	/// <summary>
	/// Represents a Memcached node in the pool.
	/// </summary>
	[DebuggerDisplay("{{MemcachedNode [ Address: {EndPoint}, IsAlive = {IsAlive} ]}}")]
	public class MemcachedNode : IMemcachedNode
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(MemcachedNode));
		private static readonly object SyncRoot = new Object();

		private bool isDisposed;

		private IPEndPoint endPoint;
		private ISocketPoolConfiguration config;
		private InternalPoolImpl internalPoolImpl;

		public MemcachedNode(IPEndPoint endpoint, ISocketPoolConfiguration socketPoolConfig)
		{
			this.endPoint = endpoint;
			this.config = socketPoolConfig;

			if (socketPoolConfig.ConnectionTimeout.TotalMilliseconds >= Int32.MaxValue)
				throw new InvalidOperationException("ConnectionTimeout must be < Int32.MaxValue");

			this.internalPoolImpl = new InternalPoolImpl(this, socketPoolConfig);
		}

		public event Action<IMemcachedNode> Failed;

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
		/// If the server is back online, we'll ercreate the internal socket pool and mark the server as alive so operations can target it.
		/// </summary>
		/// <returns>true if the server is alive; false otherwise.</returns>
		public bool Ping()
		{
			// is the server working?
			if (this.internalPoolImpl.IsAlive)
				return true;

			// this codepath is (should be) called very rarely
			// if you get here hundreds of times then you have bigger issues
			// and try to make the memcached instaces more stable and/or increase the deadTimeout
			try
			{
				// try to connect to the server
				using (var socket = this.CreateSocket()) ;

				// we could connect to the server, let's recreate the socket pool
				lock (SyncRoot)
				{
					if (this.internalPoolImpl.IsAlive)
						return true;

					// it's easier to create a new pool than reinitializing a dead one
					// rewrite-then-dispose to avoid a race condition with Acquire (which does no locking)
					var oldPool = this.internalPoolImpl;
					var newPool = new InternalPoolImpl(this, this.config);

					Interlocked.Exchange(ref this.internalPoolImpl, newPool);

					try { oldPool.Dispose(); }
					catch { }
				}

				return true;
			}
			//could not reconnect
			catch { return false; }
		}

		/// <summary>
		/// Acquires a new item from the pool
		/// </summary>
		/// <returns>An <see cref="T:PooledSocket"/> instance which is connected to the memcached server, or <value>null</value> if the pool is dead.</returns>
		public PooledSocket Acquire()
		{
			return this.internalPoolImpl.Acquire();
		}

		~MemcachedNode()
		{
			try { ((IDisposable)this).Dispose(); }
			catch { }
		}

		/// <summary>
		/// Releases all resources allocated by this instance
		/// </summary>
		public void Dispose()
		{
			if (this.isDisposed) return;

			GC.SuppressFinalize(this);

			// this is not a graceful shutdown
			// if someone uses a pooled item then it's 99% that an exception will be thrown
			// somewhere. But since the dispose is mostly used when everyone else is finished
			// this should not kill any kittens
			lock (SyncRoot)
			{
				if (this.isDisposed) return;

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
			private static log4net.ILog log = log4net.LogManager.GetLogger(typeof(InternalPoolImpl).FullName.Replace("+", "."));

			/// <summary>
			/// A list of already connected but free to use sockets
			/// </summary>
			private InterlockedQueue<PooledSocket> freeItems;

			private bool isDisposed;
			private bool isAlive;
			private DateTime markedAsDeadUtc;

			private int minItems;
			private int maxItems;

			private MemcachedNode ownerNode;
			private IPEndPoint endPoint;
			private ISocketPoolConfiguration config;
			private Semaphore semaphore;

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

				this.semaphore = new Semaphore(minItems, maxItems);
				this.freeItems = new InterlockedQueue<PooledSocket>();

				this.InitPool();
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

					if (log.IsDebugEnabled)
						log.DebugFormat("Pool has been inited for {0} with {1} sockets", this.endPoint, this.minItems);

				}
				catch (Exception e)
				{
					log.Error("Could not init pool.", e);

					this.MarkAsDead();
				}
			}

			private PooledSocket CreateSocket()
			{
				var ps = this.ownerNode.CreateSocket();
				ps.CleanupCallback = this.ReleaseSocket;

				return ps;
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

				// maybe we died while waiting
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

				var f = this.ownerNode.Failed;

				if (f != null)
					f(this.ownerNode);
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
					// one of our previous sockets has died, so probably all of them 
					// are dead. so, kill the socket (this will eventually clear the pool as well)
					socket.Destroy();
				}
			}


			~InternalPoolImpl()
			{
				try { ((IDisposable)this).Dispose(); }
				catch { }
			}

			/// <summary>
			/// Releases all resources allocated by this instance
			/// </summary>
			public void Dispose()
			{
				if (!this.isDisposed)
				{
					// this is not a graceful shutdown
					// if someone uses a pooled item then 99% that an exception will be thrown
					// somewhere. But since the dispose is mostly used when everyone else is finished
					// this should not kill any kittens
					lock (this)
					{
						if (!this.isDisposed)
						{
							this.isAlive = false;
							this.isDisposed = true;

							PooledSocket ps;

							while (this.freeItems.Dequeue(out ps))
							{
								try { ps.Destroy(); }
								catch { }
							}

							this.ownerNode = null;
							this.semaphore.Close();
							this.semaphore = null;
							this.freeItems = null;
						}
					}
				}
			}

			void IDisposable.Dispose()
			{
				this.Dispose();
			}
		}
		#endregion
		#region [ Comparer                     ]
		internal sealed class Comparer : IEqualityComparer<IMemcachedNode>
		{
			public static readonly Comparer Instance = new Comparer();

			bool IEqualityComparer<IMemcachedNode>.Equals(IMemcachedNode x, IMemcachedNode y)
			{
				return x.EndPoint.Equals(y.EndPoint);
			}

			int IEqualityComparer<IMemcachedNode>.GetHashCode(IMemcachedNode obj)
			{
				return obj.EndPoint.GetHashCode();
			}
		}
		#endregion

		protected internal virtual PooledSocket CreateSocket()
		{
			PooledSocket retval = new PooledSocket(this.endPoint, this.config.ConnectionTimeout, this.config.ReceiveTimeout);

			return retval;
		}

		protected virtual bool ExecuteOperation(IOperation op)
		{
			using (var ps = this.Acquire())
			{
				try
				{
					if (ps == null) return false;

					ps.Write(op.GetBuffer());

					return op.ReadResponse(ps);
				}
				catch (Exception e)
				{
					log.Error(e);

					return false;
				}
			}
		}

		#region [ IMemcachedNode               ]

		IPEndPoint IMemcachedNode.EndPoint
		{
			get { return this.EndPoint; }
		}

		bool IMemcachedNode.IsAlive
		{
			get { return this.IsAlive; }
		}

		bool IMemcachedNode.Ping()
		{
			return this.Ping();
		}

		bool IMemcachedNode.Execute(IOperation op)
		{
			return this.ExecuteOperation(op);
		}

		//IAsyncResult IMemcachedNode.BeginExecute(IOperation op, AsyncCallback callback, object state)
		//{
		//    throw new NotImplementedException();
		//}

		//bool IMemcachedNode.EndExecute(IAsyncResult result)
		//{
		//    throw new NotImplementedException();
		//}

		event Action<IMemcachedNode> IMemcachedNode.Failed
		{
			add { this.Failed += value; }
			remove { this.Failed -= value; }
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
