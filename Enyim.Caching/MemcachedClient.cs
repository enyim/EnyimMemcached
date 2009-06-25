using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Security.Cryptography;
using System.Globalization;
using System.Threading;
using Enyim.Caching.Memcached;
using Enyim.Caching.Configuration;
using System.Configuration;

namespace Enyim.Caching
{
	/// <summary>
	/// Memcached client.
	/// </summary>
	public sealed class MemcachedClient : IDisposable
	{
		/// <summary>
		/// Represents a value whihc indicates that an item should never expire.
		/// </summary>
		public static readonly TimeSpan Infinite = TimeSpan.Zero;

		private ServerPool pool;

		/// <summary>
		/// Initializes a new MemcachedClient instance using the default configuration section (enyim/memcached).
		/// </summary>
		public MemcachedClient()
		{
			this.pool = new ServerPool();
		}

		/// <summary>
		/// Initializes a new MemcachedClient instance using the specified configuration section. 
		/// This overload allows to create multiple MemcachedClients with different pool configurations.
		/// </summary>
		/// <param name="sectionName">The name of the configuration section to be used for configuring the behavior of the client.</param>
		public MemcachedClient(string sectionName)
		{
			MemcachedClientSection section = (MemcachedClientSection)ConfigurationManager.GetSection(sectionName);
			if (section == null)
				throw new ConfigurationErrorsException("Section " + sectionName + " is not found.");

			this.pool = new ServerPool(section);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:MemcachedClient"/> using the specified configuration.
		/// </summary>
		/// <param name="configuration">The client configuration.</param>
		public MemcachedClient(IMemcachedClientConfiguration configuration)
		{
			if (configuration == null)
				throw new ArgumentNullException("configuration");

			this.pool = new ServerPool(configuration);
		}
		
		/// <summary>
		/// Removes the specified item from the cache.
		/// </summary>
		/// <param name="key">The identifier for the item to delete.</param>
		/// <returns>true if the item was successfully removed from the cache; false otherwise.</returns>
		public bool Remove(string key)
		{
			using (DeleteOperation d = new DeleteOperation(this.pool, key))
			{
				d.Execute();

				return d.Success;
			}
		}

		/// <summary>
		/// Retrieves the specified item from the cache.
		/// </summary>
		/// <param name="key">The identifier for the item to retrieve.</param>
		/// <returns>The retrieved item, or <value>null</value> if the key was not found.</returns>
		public object Get(string key)
		{
			using (GetOperation g = new GetOperation(this.pool, key))
			{
				g.Execute();

				return g.Result;
			}
		}

		/// <summary>
		/// Retrieves the specified item from the cache.
		/// </summary>
		/// <param name="key">The identifier for the item to retrieve.</param>
		/// <returns>The retrieved item, or <value>null</value> if the key was not found.</returns>
		public T Get<T>(string key)
		{
			object retval = this.Get(key);

			if (retval == null || !(retval is T))
				return default(T);

			return (T)retval;
		}

		/// <summary>
		/// Increments the value of the specified key by the given amount. The operation is atomic and happens on the server.
		/// </summary>
		/// <param name="key">The identifier for the item to increment.</param>
		/// <param name="amount">The amount by which the client wants to increase the item.</param>
		/// <returns>The new value of the item or -1 if not found.</returns>
		/// <remarks>The item must be inserted into the cache before it can be changed. The item must be inserted as a <see cref="T:System.String"/>. The operation only works with <see cref="System.UInt32"/> values, so -1 always indicates that the item was not found.</remarks>
		public long Increment(string key, uint amount)
		{
			using (IncrementOperation i = new IncrementOperation(this.pool, key, amount))
			{
				i.Execute();

				return i.Success ? (long)i.Result : -1;
			}
		}

		/// <summary>
		/// Increments the value of the specified key by the given amount. The operation is atomic and happens on the server.
		/// </summary>
		/// <param name="key">The identifier for the item to increment.</param>
		/// <param name="amount">The amount by which the client wants to decrease the item.</param>
		/// <returns>The new value of the item or -1 if not found.</returns>
		/// <remarks>The item must be inserted into the cache before it can be changed. The item must be inserted as a <see cref="T:System.String"/>. The operation only works with <see cref="System.UInt32"/> values, so -1 always indicates that the item was not found.</remarks>
		public long Decrement(string key, uint amount)
		{
			using (DecrementOperation d = new DecrementOperation(this.pool, key, amount))
			{
				d.Execute();

				return d.Success ? (long)d.Result : -1;
			}
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
			return MemcachedClient.Store(this.pool, (StoreCommand)mode, key, value, 0, MemcachedClient.Infinite, DateTime.MinValue);
		}

		/// <summary>
		/// Inserts a range of bytes (usually memory area or serialized data) into the cache with a cache key to reference its location.
		/// </summary>
		/// <param name="mode">Defines how the item is stored in the cache.</param>
		/// <param name="key">The key used to reference the item.</param>
		/// <param name="value">The data to be stored.</param>
		/// <param name="offset">A 32 bit integer that represents the index of the first byte to store.</param>
		/// <param name="length">A 32 bit integer that represents the number of bytes to store.</param>
		/// <remarks>The item does not expire unless it is removed due memory pressure.</remarks>
		/// <returns>true if the item was successfully stored in the cache; false otherwise.</returns>
		public bool Store(StoreMode mode, string key, byte[] value, int offset, int length)
		{
			return MemcachedClient.Store(this.pool, (StoreCommand)mode, key, value, 0, offset, length, MemcachedClient.Infinite, DateTime.MinValue);
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
			return MemcachedClient.Store(this.pool, (StoreCommand)mode, key, value, 0, validFor, DateTime.MinValue);
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
			return MemcachedClient.Store(this.pool, (StoreCommand)mode, key, value, 0, TimeSpan.MinValue, expiresAt);
		}

		/// <summary>
		/// Inserts a range of bytes (usually memory area or serialized data) into the cache with a cache key to reference its location.
		/// </summary>
		/// <param name="mode">Defines how the item is stored in the cache.</param>
		/// <param name="key">The key used to reference the item.</param>
		/// <param name="value">The data to be stored.</param>
		/// <param name="offset">A 32 bit integer that represents the index of the first byte to store.</param>
		/// <param name="length">A 32 bit integer that represents the number of bytes to store.</param>
		/// <param name="validFor">The interval after the item is invalidated in the cache.</param>
		/// <returns>true if the item was successfully stored in the cache; false otherwise.</returns>
		public bool Store(StoreMode mode, string key, byte[] value, int offset, int length, TimeSpan validFor)
		{
			return MemcachedClient.Store(this.pool, (StoreCommand)mode, key, value, 0, offset, length, validFor, DateTime.MinValue);
		}

		/// <summary>
		/// Inserts a range of bytes (usually memory area or serialized data) into the cache with a cache key to reference its location.
		/// </summary>
		/// <param name="mode">Defines how the item is stored in the cache.</param>
		/// <param name="key">The key used to reference the item.</param>
		/// <param name="value">The data to be stored.</param>
		/// <param name="offset">A 32 bit integer that represents the index of the first byte to store.</param>
		/// <param name="length">A 32 bit integer that represents the number of bytes to store.</param>
		/// <param name="expiresAt">The time when the item is invalidated in the cache.</param>
		/// <returns>true if the item was successfully stored in the cache; false otherwise.</returns>
		public bool Store(StoreMode mode, string key, byte[] value, int offset, int length, DateTime expiresAt)
		{
			return MemcachedClient.Store(this.pool, (StoreCommand)mode, key, value, 0, offset, length, TimeSpan.MinValue, expiresAt);
		}

		/// <summary>
		/// Appends the data to the end of the specified item's data.
		/// </summary>
		/// <param name="key">The key used to reference the item.</param>
		/// <param name="data">The data to be stored.</param>
		/// <returns>true if the data was successfully stored; false otherwise.</returns>
		public bool Append(string key, byte[] data)
		{
			return MemcachedClient.Store(this.pool, StoreCommand.Append, key, data, 0, MemcachedClient.Infinite, DateTime.MinValue);
		}

		/// <summary>
		/// Inserts the data before the specified item's data.
		/// </summary>
		/// <returns>true if the data was successfully stored; false otherwise.</returns>
		public bool Prepend(string key, byte[] data)
		{
			return MemcachedClient.Store(this.pool, StoreCommand.Prepend, key, data, 0, MemcachedClient.Infinite, DateTime.MinValue);
		}

		/// <summary>
		/// Updates an item in the cache with a cache key to reference its location, but only if it has not been changed since the last retrieval. The invoker must pass in the value returned by <see cref="M:MultiGet"/> called "cas" value. If this value matches the server's value, the item will be updated; otherwise the update fails.
		/// </summary>
		/// <param name="key">The key used to reference the item.</param>
		/// <param name="value">The object to be inserted into the cache.</param>
		/// <param name="cas">The unique value returned by <see cref="M:MultiGet"/>.</param>
		/// <remarks>The item does not expire unless it is removed due memory pressure.</remarks>
		public bool CheckAndSet(string key, object value, ulong cas)
		{
			return MemcachedClient.Store(this.pool, StoreCommand.CheckAndSet, key, value, cas, MemcachedClient.Infinite, DateTime.MinValue);
		}

		/// <summary>
		/// Updates an item in the cache with a cache key to reference its location, but only if it has not been changed since the last retrieval. The invoker must pass in the value returned by <see cref="M:MultiGet"/> called "cas" value. If this value matches the server's value, the item will be updated; otherwise the update fails.
		/// </summary>
		/// <param name="key">The key used to reference the item.</param>
		/// <param name="value">The data to be stored.</param>
		/// <param name="offset">A 32 bit integer that represents the index of the first byte to store.</param>
		/// <param name="length">A 32 bit integer that represents the number of bytes to store.</param>
		/// <param name="cas">The unique value returned by <see cref="M:MultiGet"/>.</param>
		/// <remarks>The item does not expire unless it is removed due memory pressure.</remarks>
		public bool CheckAndSet(string key, byte[] value, int offset, int length, ulong cas)
		{
			return MemcachedClient.Store(this.pool, StoreCommand.CheckAndSet, key, value, cas, offset, length, MemcachedClient.Infinite, DateTime.MinValue);
		}

		/// <summary>
		/// Updates an item in the cache with a cache key to reference its location, but only if it has not been changed since the last retrieval. The invoker must pass in the value returned by <see cref="M:MultiGet"/> called "cas" value. If this value matches the server's value, the item will be updated; otherwise the update fails.
		/// </summary>
		/// <param name="key">The key used to reference the item.</param>
		/// <param name="value">The object to be inserted into the cache.</param>
		/// <param name="cas">The unique value returned by <see cref="M:MultiGet"/>.</param>
		/// <param name="validFor">The interval after the item is invalidated in the cache.</param>
		public bool CheckAndSet(string key, object value, ulong cas, TimeSpan validFor)
		{
			return MemcachedClient.Store(this.pool, StoreCommand.CheckAndSet, key, value, cas, validFor, DateTime.MinValue);
		}

		/// <summary>
		/// Updates an item in the cache with a cache key to reference its location, but only if it has not been changed since the last retrieval. The invoker must pass in the value returned by <see cref="M:MultiGet"/> called "cas" value. If this value matches the server's value, the item will be updated; otherwise the update fails.
		/// </summary>
		/// <param name="key">The key used to reference the item.</param>
		/// <param name="value">The object to be inserted into the cache.</param>
		/// <param name="cas">The unique value returned by <see cref="M:MultiGet"/>.</param>
		/// <param name="expiresAt">The time when the item is invalidated in the cache.</param>
		public bool CheckAndSet(string key, object value, ulong cas, DateTime expiresAt)
		{
			return MemcachedClient.Store(this.pool, StoreCommand.CheckAndSet, key, value, cas, TimeSpan.MinValue, expiresAt);
		}

		/// <summary>
		/// Updates an item in the cache with a cache key to reference its location, but only if it has not been changed since the last retrieval. The invoker must pass in the value returned by <see cref="M:MultiGet"/> called "cas" value. If this value matches the server's value, the item will be updated; otherwise the update fails.
		/// </summary>
		/// <param name="key">The key used to reference the item.</param>
		/// <param name="value">The data to be stored.</param>
		/// <param name="offset">A 32 bit integer that represents the index of the first byte to store.</param>
		/// <param name="length">A 32 bit integer that represents the number of bytes to store.</param>
		/// <param name="cas">The unique value returned by <see cref="M:MultiGet"/>.</param>
		/// <param name="validFor">The interval after the item is invalidated in the cache.</param>
		public bool CheckAndSet(string key, byte[] value, int offset, int length, ulong cas, TimeSpan validFor)
		{
			return MemcachedClient.Store(this.pool, StoreCommand.CheckAndSet, key, value, cas, offset, length, validFor, DateTime.MinValue);
		}

		/// <summary>
		/// Updates an item in the cache with a cache key to reference its location, but only if it has not been changed since the last retrieval. The invoker must pass in the value returned by <see cref="M:MultiGet"/> called "cas" value. If this value matches the server's value, the item will be updated; otherwise the update fails.
		/// </summary>
		/// <param name="key">The key used to reference the item.</param>
		/// <param name="value">The data to be stored.</param>
		/// <param name="offset">A 32 bit integer that represents the index of the first byte to store.</param>
		/// <param name="length">A 32 bit integer that represents the number of bytes to store.</param>
		/// <param name="cas">The unique value returned by <see cref="M:MultiGet"/>.</param>
		/// <param name="expiresAt">The time when the item is invalidated in the cache.</param>
		public bool CheckAndSet(string key, byte[] value, int offset, int length, ulong cas, DateTime expiresAt)
		{
			return MemcachedClient.Store(this.pool, StoreCommand.CheckAndSet, key, value, cas, offset, length, TimeSpan.MinValue, expiresAt);
		}

		/// <summary>
		/// Removes all data from the cache.
		/// </summary>
		public void FlushAll()
		{
			using (FlushOperation f = new FlushOperation(this.pool))
			{
				f.Execute();
			}
		}

		/// <summary>
		/// Returns statistics about the servers.
		/// </summary>
		/// <returns></returns>
		public ServerStats Stats()
		{
			using (StatsOperation s = new StatsOperation(this.pool))
			{
				s.Execute();

				return s.Results;
			}
		}

		/// <summary>
		/// Retrieves multiple items from the cache.
		/// </summary>
		/// <param name="keys">The list of identifiers for the items to retrieve.</param>
		/// <returns>a Dictionary holding all items indexed by their key.</returns>
		public IDictionary<string, object> Get(IEnumerable<string> keys)
		{
			IDictionary<string, ulong> tmp;

			return Get(keys, out tmp);
		}

		/// <summary>
		/// Retrieves multiple items from the cache.
		/// </summary>
		/// <param name="keys">The list of identifiers for the items to retrieve.</param>
		/// <param name="casValues">The CAS values for the keys.</param>
		/// <returns>a Dictionary holding all items indexed by their key.</returns>
		public IDictionary<string, object> Get(IEnumerable<string> keys, out IDictionary<string, ulong> casValues)
		{
			using (MultiGetOperation mgo = new MultiGetOperation(this.pool, keys))
			{
				mgo.Execute();

				casValues = mgo.CasValues ?? new Dictionary<string, ulong>();

				return mgo.Result ?? new Dictionary<string, object>();
			}
		}

		#region [ Store                        ]
		private static bool Store(ServerPool pool, StoreCommand mode, string key, object value, ulong casValue, TimeSpan validFor, DateTime expiresAt)
		{
			if (value == null)
				return false;

			using (StoreOperation s = new StoreOperation(pool, mode, key, value, casValue, validFor, expiresAt))
			{
				s.Execute();

				return s.Success;
			}
		}

		private static bool Store(ServerPool pool, StoreCommand mode, string key, byte[] value, ulong casValue, int offset, int length, TimeSpan validFor, DateTime expiresAt)
		{
			if (value == null)
				return false;

			using (StoreOperation s = new StoreOperation(pool, mode, key, new ArraySegment<byte>(value, offset, length), casValue, validFor, expiresAt))
			{
				s.Execute();

				return s.Success;
			}
		}
		#endregion

		void IDisposable.Dispose()
		{
			this.Dispose();
		}

		/// <summary>
		/// Releases all resources allocated by this instance
		/// </summary>
		/// <remarks>Technically it's not really neccesary to call this, since the client does not create "really" disposable objects, so it's safe to assume that when the AppPool shuts down all resources will be released correctly and no handles or such will remain in the memory.</remarks>
		public void Dispose()
		{
			if (this.pool == null)
				throw new ObjectDisposedException("MemcachedClient");

			lock (this)
			{
				((IDisposable)this.pool).Dispose();
				this.pool = null;
			}
		}
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