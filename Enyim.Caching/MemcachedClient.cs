using System;
using System.Linq;
using System.Configuration;
using Enyim.Caching.Configuration;
using Enyim.Caching.Memcached;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.Diagnostics;

namespace Enyim.Caching
{
	/// <summary>
	/// Memcached client.
	/// </summary>
	public class MemcachedClient : IDisposable
	{
		/// <summary>
		/// Represents a value which indicates that an item should never expire.
		/// </summary>
		public static readonly TimeSpan Infinite = TimeSpan.Zero;
		internal static readonly MemcachedClientSection DefaultSettings = ConfigurationManager.GetSection("enyim.com/memcached") as MemcachedClientSection;
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(MemcachedClient));

		private IServerPool pool;
		private IMemcachedKeyTransformer keyTransformer;
		private ITranscoder transcoder;

		/// <summary>
		/// Initializes a new MemcachedClient instance using the default configuration section (enyim/memcached).
		/// </summary>
		public MemcachedClient() : this(DefaultSettings) { }

		/// <summary>
		/// Initializes a new MemcachedClient instance using the specified configuration section. 
		/// This overload allows to create multiple MemcachedClients with different pool configurations.
		/// </summary>
		/// <param name="sectionName">The name of the configuration section to be used for configuring the behavior of the client.</param>
		public MemcachedClient(string sectionName) : this(GetSection(sectionName)) { }

		private static IMemcachedClientConfiguration GetSection(string sectionName)
		{
			MemcachedClientSection section = (MemcachedClientSection)ConfigurationManager.GetSection(sectionName);
			if (section == null)
				throw new ConfigurationErrorsException("Section " + sectionName + " is not found.");

			return section;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:MemcachedClient"/> using the specified configuration instance.
		/// </summary>
		/// <param name="configuration">The client configuration.</param>
		public MemcachedClient(IMemcachedClientConfiguration configuration)
		{
			if (configuration == null)
				throw new ArgumentNullException("configuration");

			this.keyTransformer = configuration.CreateKeyTransformer() ?? new DefaultKeyTransformer();
			this.transcoder = configuration.CreateTranscoder() ?? new DefaultTranscoder();

			this.pool = configuration.CreatePool();
			this.pool.Start();
		}

		public MemcachedClient(IServerPool pool, IMemcachedKeyTransformer keyTransformer, ITranscoder transcoder)
		{
			if (pool == null) throw new ArgumentNullException("pool");
			if (keyTransformer == null) throw new ArgumentNullException("keyTransformer");
			if (transcoder == null) throw new ArgumentNullException("transcoder");

			this.keyTransformer = keyTransformer;
			this.transcoder = transcoder;

			this.pool = pool;
			this.pool.Start();
		}

		~MemcachedClient()
		{
			try { ((IDisposable)this).Dispose(); }
			catch { }
		}

		/// <summary>
		/// Retrieves the specified item from the cache.
		/// </summary>
		/// <param name="key">The identifier for the item to retrieve.</param>
		/// <returns>The retrieved item, or <value>null</value> if the key was not found.</returns>
		public object Get(string key)
		{
			object tmp;

			return this.TryGet(key, out tmp) ? tmp : null;
		}

		/// <summary>
		/// Retrieves the specified item from the cache.
		/// </summary>
		/// <param name="key">The identifier for the item to retrieve.</param>
		/// <returns>The retrieved item, or <value>default(T)</value> if the key was not found.</returns>
		public T Get<T>(string key)
		{
			object tmp;

			return TryGet(key, out tmp) ? (T)tmp : default(T);
		}

		/// <summary>
		/// Tries to get an item from the cache.
		/// </summary>
		/// <param name="key">The identifier for the item to retrieve.</param>
		/// <param name="value">The retrieved item or null if not found.</param>
		/// <returns>The <value>true</value> if the item was successfully retrieved.</returns>
		public bool TryGet(string key, out object value)
		{
			var hashedKey = this.keyTransformer.Transform(key);
			var node = this.pool.Locate(hashedKey);

			if (node != null)
			{
				var command = this.pool.OperationFactory.Get(hashedKey);
				if (node.Execute(command))
				{
					value = this.transcoder.Deserialize(command.Result);

					return true;
				}
			}

			value = null;

			return false;
		}

		/// <summary>
		/// Inserts an item into the cache with a cache key to reference its location.
		/// </summary>
		/// <param name="mode">Defines how the item is stored in the cache.</param>
		/// <param name="key">The key used to reference the item.</param>
		/// <param name="value">The object to be inserted into the cache.</param>
		/// <remarks>The item does not expire unless it is removed due memory pressure.</remarks>
		/// <returns>true if the item was successfully stored in the cache; false otherwise.</returns>
		public bool Store(StoreMode mode, string key, object value)
		{
			return this.Store(mode, key, value, 0);
		}

		/// <summary>
		/// Inserts an item into the cache with a cache key to reference its location.
		/// </summary>
		/// <param name="mode">Defines how the item is stored in the cache.</param>
		/// <param name="key">The key used to reference the item.</param>
		/// <param name="value">The object to be inserted into the cache.</param>
		/// <param name="validFor">The interval after the item is invalidated in the cache.</param>
		/// <returns>true if the item was successfully stored in the cache; false otherwise.</returns>
		public bool Store(StoreMode mode, string key, object value, TimeSpan validFor)
		{
			return this.Store(mode, key, value, MemcachedClient.GetExpiration(validFor, null));
		}

		/// <summary>
		/// Inserts an item into the cache with a cache key to reference its location.
		/// </summary>
		/// <param name="mode">Defines how the item is stored in the cache.</param>
		/// <param name="key">The key used to reference the item.</param>
		/// <param name="value">The object to be inserted into the cache.</param>
		/// <param name="expiresAt">The time when the item is invalidated in the cache.</param>
		/// <returns>true if the item was successfully stored in the cache; false otherwise.</returns>
		public bool Store(StoreMode mode, string key, object value, DateTime expiresAt)
		{
			return this.Store(mode, key, value, MemcachedClient.GetExpiration(null, expiresAt));
		}

		private bool Store(StoreMode mode, string key, object value, uint expires)
		{
			var hashedKey = this.keyTransformer.Transform(key);
			var node = this.pool.Locate(hashedKey);

			if (node != null)
			{
				CacheItem item;

				try { item = this.transcoder.Serialize(value); }
				catch (Exception e)
				{
					log.Error(e);

					return false;
				}

				var command = this.pool.OperationFactory.Store(mode, hashedKey, item, expires);

				return node.Execute(command);
			}

			return false;
		}

		/// <summary>
		/// Increments the value of the specified key by the given amount. The operation is atomic and happens on the server.
		/// </summary>
		/// <param name="key">The identifier for the item to increment.</param>
		/// <param name="defaultValue">The value which will be stored by the server if the specified item was not found.</param>
		/// <param name="delta">The amount by which the client wants to increase the item.</param>
		/// <returns>The new value of the item or defaultValue if the key was not found.</returns>
		/// <remarks>If the client uses the Text protocol, the item must be inserted into the cache before it can be changed. It must be inserted as a <see cref="T:System.String"/>. Moreover the Text protocol only works with <see cref="System.UInt32"/> values, so return value -1 always indicates that the item was not found.</remarks>
		public ulong Increment(string key, ulong defaultValue, ulong delta)
		{
			return this.Mutate(MutationMode.Increment, key, defaultValue, delta, 0);
		}

		/// <summary>
		/// Increments the value of the specified key by the given amount. The operation is atomic and happens on the server.
		/// </summary>
		/// <param name="key">The identifier for the item to increment.</param>
		/// <param name="defaultValue">The value which will be stored by the server if the specified item was not found.</param>
		/// <param name="delta">The amount by which the client wants to increase the item.</param>
		/// <param name="validFor">The interval after the item is invalidated in the cache.</param>
		/// <returns>The new value of the item or defaultValue if the key was not found.</returns>
		/// <remarks>If the client uses the Text protocol, the item must be inserted into the cache before it can be changed. It must be inserted as a <see cref="T:System.String"/>. Moreover the Text protocol only works with <see cref="System.UInt32"/> values, so return value -1 always indicates that the item was not found.</remarks>
		public ulong Increment(string key, ulong defaultValue, ulong delta, TimeSpan validFor)
		{
			return this.Mutate(MutationMode.Increment, key, defaultValue, delta, MemcachedClient.GetExpiration(validFor, null));
		}

		/// <summary>
		/// Increments the value of the specified key by the given amount. The operation is atomic and happens on the server.
		/// </summary>
		/// <param name="key">The identifier for the item to increment.</param>
		/// <param name="defaultValue">The value which will be stored by the server if the specified item was not found.</param>
		/// <param name="delta">The amount by which the client wants to increase the item.</param>
		/// <param name="expiresAt">The time when the item is invalidated in the cache.</param>
		/// <returns>The new value of the item or defaultValue if the key was not found.</returns>
		/// <remarks>If the client uses the Text protocol, the item must be inserted into the cache before it can be changed. It must be inserted as a <see cref="T:System.String"/>. Moreover the Text protocol only works with <see cref="System.UInt32"/> values, so return value -1 always indicates that the item was not found.</remarks>
		public ulong Increment(string key, ulong defaultValue, ulong delta, DateTime expiresAt)
		{
			return this.Mutate(MutationMode.Increment, key, defaultValue, delta, MemcachedClient.GetExpiration(null, expiresAt));
		}

		/// <summary>
		/// Decrements the value of the specified key by the given amount. The operation is atomic and happens on the server.
		/// </summary>
		/// <param name="key">The identifier for the item to decrement.</param>
		/// <param name="defaultValue">The value which will be stored by the server if the specified item was not found.</param>
		/// <param name="delta">The amount by which the client wants to decrease the item.</param>
		/// <returns>The new value of the item or defaultValue if the key was not found.</returns>
		/// <remarks>If the client uses the Text protocol, the item must be inserted into the cache before it can be changed. It must be inserted as a <see cref="T:System.String"/>. Moreover the Text protocol only works with <see cref="System.UInt32"/> values, so return value -1 always indicates that the item was not found.</remarks>
		public ulong Decrement(string key, ulong defaultValue, ulong delta)
		{
			return this.Mutate(MutationMode.Decrement, key, defaultValue, delta, 0);
		}

		/// <summary>
		/// Decrements the value of the specified key by the given amount. The operation is atomic and happens on the server.
		/// </summary>
		/// <param name="key">The identifier for the item to decrement.</param>
		/// <param name="defaultValue">The value which will be stored by the server if the specified item was not found.</param>
		/// <param name="delta">The amount by which the client wants to decrease the item.</param>
		/// <param name="validFor">The interval after the item is invalidated in the cache.</param>
		/// <returns>The new value of the item or defaultValue if the key was not found.</returns>
		/// <remarks>If the client uses the Text protocol, the item must be inserted into the cache before it can be changed. It must be inserted as a <see cref="T:System.String"/>. Moreover the Text protocol only works with <see cref="System.UInt32"/> values, so return value -1 always indicates that the item was not found.</remarks>
		public ulong Decrement(string key, ulong defaultValue, ulong delta, TimeSpan validFor)
		{
			return this.Mutate(MutationMode.Decrement, key, defaultValue, delta, MemcachedClient.GetExpiration(validFor, null));
		}

		/// <summary>
		/// Decrements the value of the specified key by the given amount. The operation is atomic and happens on the server.
		/// </summary>
		/// <param name="key">The identifier for the item to decrement.</param>
		/// <param name="defaultValue">The value which will be stored by the server if the specified item was not found.</param>
		/// <param name="delta">The amount by which the client wants to decrease the item.</param>
		/// <param name="expiresAt">The time when the item is invalidated in the cache.</param>
		/// <returns>The new value of the item or defaultValue if the key was not found.</returns>
		/// <remarks>If the client uses the Text protocol, the item must be inserted into the cache before it can be changed. It must be inserted as a <see cref="T:System.String"/>. Moreover the Text protocol only works with <see cref="System.UInt32"/> values, so return value -1 always indicates that the item was not found.</remarks>
		public ulong Decrement(string key, ulong defaultValue, ulong delta, DateTime expiresAt)
		{
			return this.Mutate(MutationMode.Decrement, key, defaultValue, delta, MemcachedClient.GetExpiration(null, expiresAt));
		}

		private ulong Mutate(MutationMode mode, string key, ulong defaultValue, ulong delta, uint expires)
		{
			var hashedKey = this.keyTransformer.Transform(key);
			var node = this.pool.Locate(hashedKey);

			if (node != null)
			{
				var command = this.pool.OperationFactory.Mutate(mode, hashedKey, defaultValue, delta, expires);

				if (node.Execute(command))
					return command.Result;
			}

			return 0;
		}

		/// <summary>
		/// Appends the data to the end of the specified item's data on the server.
		/// </summary>
		/// <param name="key">The key used to reference the item.</param>
		/// <param name="data">The data to be stored.</param>
		/// <returns>true if the data was successfully stored; false otherwise.</returns>
		public bool Append(string key, ArraySegment<byte> data)
		{
			return this.Concatenate(ConcatenationMode.Append, key, data);
		}

		/// <summary>
		/// Inserts the data before the specified item's data on the server.
		/// </summary>
		/// <returns>true if the data was successfully stored; false otherwise.</returns>
		public bool Prepend(string key, ArraySegment<byte> data)
		{
			return this.Concatenate(ConcatenationMode.Prepend, key, data);
		}

		private bool Concatenate(ConcatenationMode mode, string key, ArraySegment<byte> data)
		{
			var hashedKey = this.keyTransformer.Transform(key);
			var node = this.pool.Locate(hashedKey);

			if (node != null)
			{
				var command = this.pool.OperationFactory.Concat(mode, hashedKey, data);

				return node.Execute(command);
			}

			return false;
		}

		/// <summary>
		/// Removes all data from the cache. Note: this will invalidate all data on all servers in the pool.
		/// </summary>
		public void FlushAll()
		{
			foreach (var node in this.pool.GetWorkingNodes())
			{
				var command = this.pool.OperationFactory.Flush();

				node.Execute(command);
			}
		}

		/// <summary>
		/// Returns statistics about the servers.
		/// </summary>
		/// <returns></returns>
		public ServerStats Stats()
		{
			var results = new Dictionary<IPEndPoint, Dictionary<string, string>>();
			var handles = new List<WaitHandle>();

			foreach (var node in this.pool.GetWorkingNodes())
			{
				var cmd = this.pool.OperationFactory.Stats();
				var mre = new ManualResetEvent(false);

				Func<IOperation, bool> action = new Func<IOperation, bool>(node.Execute);

				action.BeginInvoke(cmd, iar =>
				{
					action.EndInvoke(iar);

					lock (results)
						results[node.EndPoint] = cmd.Result;

					mre.Set();
				}, null);

				handles.Add(mre);
			}

			if (handles.Count > 0)
				WaitHandle.WaitAll(handles.ToArray());

			return new ServerStats(results);
		}

		/// <summary>
		/// Removes the specified item from the cache.
		/// </summary>
		/// <param name="key">The identifier for the item to delete.</param>
		/// <returns>true if the item was successfully removed from the cache; false otherwise.</returns>
		public bool Remove(string key)
		{
			var hashedKey = this.keyTransformer.Transform(key);
			var node = this.pool.Locate(hashedKey);

			if (node != null)
			{
				var command = this.pool.OperationFactory.Delete(hashedKey);

				return node.Execute(command);
			}

			return false;
		}

		/// <summary>
		/// Retrieves multiple items from the cache.
		/// </summary>
		/// <param name="keys">The list of identifiers for the items to retrieve.</param>
		/// <returns>a Dictionary holding all items indexed by their key.</returns>
		public IDictionary<string, object> Get(IEnumerable<string> keys)
		{
			// transform the keys and index them by hashed => original
			// the mget results will be mapped using this index
			var hashed = keys.ToDictionary(key => this.keyTransformer.Transform(key));
			var byServer = hashed.Keys.ToLookup(key => this.pool.Locate(key));

			var retval = new Dictionary<string, object>(hashed.Count);
			var handles = new List<WaitHandle>();
			var i = 0;

			//execute each list of keys on their respective node
			foreach (var slice in byServer)
			{
				var node = slice.Key;

				// skip the dead nodes
				if (node == null) continue;

				var nodeKeys = slice.ToArray();
				var mget = this.pool.OperationFactory.MultiGet(nodeKeys);

				// we'll use the delegate's BeginInvoke/EndInvoke to run the gets parallel
				Func<IOperation, bool> exec = new Func<IOperation, bool>(node.Execute);
				var mre = new ManualResetEvent(false);
				handles.Add(mre);

				Console.WriteLine(i + " Expecting: " + nodeKeys.Length);

				//execute the mgets in parallel
				exec.BeginInvoke(mget, iar =>
				{
					try
					{
						if (exec.EndInvoke(iar))
						{
							Console.WriteLine(iar.AsyncState + " Success: " + mget.Result.Count);
							foreach (var kvp in mget.Result)
							{
								string original;
								var tryget = hashed.TryGetValue(kvp.Key, out original);

								Debug.Assert(tryget, "MGet returned unexpected key: " + kvp.Key);

								// the lock will serialize the merges,
								// but at least the commands were not waiting on each other
								lock (retval)
									retval[original] = this.transcoder.Deserialize(kvp.Value);
							}
						}
					}
					catch (Exception e)
					{
						log.Error(e);
					}
					finally
					{
						// indicate that we finished processing
						mre.Set();
					}
				}, i);

				i++;
			}

			if (handles.Count > 0)
				WaitHandle.WaitAll(handles.ToArray());

			return retval;
		}

		#region [ Expiration helper            ]

		private const int MaxSeconds = 60 * 60 * 24 * 30;
		private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1);

		private static uint GetExpiration(TimeSpan? validFor, DateTime? expiresAt)
		{
			if (validFor != null && expiresAt != null)
				throw new ArgumentException("You cannot specify both validFor and expiresAt.");

			if (expiresAt != null)
			{
				DateTime dt = expiresAt.Value;

				if (dt < UnixEpoch)
					throw new ArgumentOutOfRangeException("expiresAt", "expiresAt must be >= 1970/1/1");

				// accept MaxValue as infinite
				if (dt == DateTime.MaxValue)
					return 0;

				uint retval = (uint)(dt.ToUniversalTime() - UnixEpoch).TotalSeconds;

				return retval;
			}

			TimeSpan ts = validFor.Value;

			// accept Zero as infinite
			if (ts.TotalSeconds >= MaxSeconds || ts < TimeSpan.Zero)
				throw new ArgumentOutOfRangeException("validFor", "validFor must be < 30 days && >= 0");

			return (uint)ts.TotalSeconds;
		}

		#endregion
		#region [ IDisposable                  ]

		void IDisposable.Dispose()
		{
			this.Dispose();
		}

		/// <summary>
		/// Releases all resources allocated by this instance
		/// </summary>
		/// <remarks>You should only call this when you are not using static instances of the client, so it can close all conections and release the sockets.</remarks>
		public void Dispose()
		{
			if (this.pool != null)
			{
				GC.SuppressFinalize(this);

				try
				{
					this.pool.Dispose();
				}
				finally
				{
					this.pool = null;
				}
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
