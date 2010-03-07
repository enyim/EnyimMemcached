using System;
using System.Configuration;
using Enyim.Caching.Configuration;
using Enyim.Caching.Memcached;

namespace Enyim.Caching
{
	/// <summary>
	/// Memcached client.
	/// </summary>
	public sealed class MemcachedClient2 : IDisposable
	{
		internal static MemcachedClientSection DefaultSettings = ConfigurationManager.GetSection("enyim.com/memcached") as MemcachedClientSection;

		private IProtocolImplementation protocol;

		/// <summary>
		/// Initializes a new MemcachedClient instance using the default configuration section (enyim/memcached).
		/// </summary>
		public MemcachedClient2()
		{
			this.Initialize(DefaultSettings);
		}

		/// <summary>
		/// Initializes a new MemcachedClient instance using the specified configuration section. 
		/// This overload allows to create multiple MemcachedClients with different pool configurations.
		/// </summary>
		/// <param name="sectionName">The name of the configuration section to be used for configuring the behavior of the client.</param>
		public MemcachedClient2(string sectionName)
		{
			MemcachedClientSection section = (MemcachedClientSection)ConfigurationManager.GetSection(sectionName);
			if (section == null)
				throw new ConfigurationErrorsException("Section " + sectionName + " is not found.");

			this.Initialize(section);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:MemcachedClient"/> using the specified configuration.
		/// </summary>
		/// <param name="configuration">The client configuration.</param>
		public MemcachedClient2(IMemcachedClientConfiguration configuration)
		{
			if (configuration == null)
				throw new ArgumentNullException("configuration");

			this.Initialize(configuration);
		}

		private static ISaslAuthenticationProvider GetProvider(IMemcachedClientConfiguration configuration)
		{
			// create&initialize the authenticator, if any
			// we'll use this single instance everywhere, so it must be thread safe
			IAuthenticationConfiguration auth = configuration.Authentication;
			if (auth != null)
			{
				Type t = auth.Type;
				var provider = (t == null) ? null : Enyim.Reflection.FastActivator.CreateInstance(t) as ISaslAuthenticationProvider;

				if (provider != null)
				{
					provider.Initialize(auth.Parameters);
					return provider;
				}
			}

			return null;
		}

		private void Initialize(IMemcachedClientConfiguration configuration)
		{
			ISaslAuthenticationProvider provider = GetProvider(configuration);

			// we'll pass ourselves as the authenticator, and we'll forward all auth calls to the protocol
			// this way we'll avoid that the pool and the protocol reference each other
			ServerPool pool = new ServerPool(configuration);

			switch (configuration.Protocol)
			{
				case MemcachedProtocol.Binary:
					this.protocol = new Enyim.Caching.Memcached.Operations.Binary.BinaryProtocol(pool, provider);
					break;

				case MemcachedProtocol.Text:
					// authentication is not supported for the text protocol
					this.protocol = new Enyim.Caching.Memcached.Operations.Text.TextProtocol(pool);
					break;

				default: throw new ArgumentOutOfRangeException("Unknown protocol: " + configuration.Protocol);
			}

			// everything is initialized, start the pool
			pool.Start();
		}

		public object Get(string key)
		{
			return this.protocol.Get(key);
		}

		public T Get<T>(string key)
		{
			object tmp;

			return TryGet(key, out tmp) ? (T)tmp : default(T);
		}

		public bool TryGet(string key, out object value)
		{
			return this.protocol.TryGet(key, out value);
		}

		public bool Store(StoreMode mode, string key, object value)
		{
			return this.protocol.Store(mode, key, value, 0);
		}

		public bool Store(StoreMode mode, string key, object value, TimeSpan validFor)
		{
			return this.protocol.Store(mode, key, value, MemcachedClient2.GetExpiration(validFor, null));
		}

		public bool Store(StoreMode mode, string key, object value, DateTime expiresAt)
		{
			return this.protocol.Store(mode, key, value, MemcachedClient2.GetExpiration(null, expiresAt));
		}

		public ulong Increment(string key, ulong defaultValue, ulong delta)
		{
			return this.protocol.Mutate(MutationMode.Increment, key, defaultValue, delta, 0);
		}

		public ulong Increment(string key, ulong defaultValue, ulong step, TimeSpan validFor)
		{
			return this.protocol.Mutate(MutationMode.Increment, key, defaultValue, step, MemcachedClient2.GetExpiration(validFor, null));
		}

		public ulong Increment(string key, ulong defaultValue, ulong step, DateTime expiresAt)
		{
			return this.protocol.Mutate(MutationMode.Increment, key, defaultValue, step, MemcachedClient2.GetExpiration(null, expiresAt));
		}

		public ulong Decrement(string key, ulong defaultValue, ulong delta)
		{
			return this.protocol.Mutate(MutationMode.Decrement, key, defaultValue, delta, 0);
		}

		public ulong Decrement(string key, ulong defaultValue, ulong step, TimeSpan validFor)
		{
			return this.protocol.Mutate(MutationMode.Decrement, key, defaultValue, step, MemcachedClient2.GetExpiration(validFor, null));
		}

		public ulong Decrement(string key, ulong defaultValue, ulong step, DateTime expiresAt)
		{
			return this.protocol.Mutate(MutationMode.Decrement, key, defaultValue, step, MemcachedClient2.GetExpiration(null, expiresAt));
		}

		public bool Append(string key, ArraySegment<byte> data)
		{
			return this.protocol.Concatenate(ConcatenationMode.Append, key, data);
		}

		public bool Prepend(string key, ArraySegment<byte> data)
		{
			return this.protocol.Concatenate(ConcatenationMode.Prepend, key, data);
		}

		public void FlushAll()
		{
			this.protocol.FlushAll();
		}

		public ServerStats Stats()
		{
			return this.protocol.Stats();
		}

		private const int MaxSeconds = 60 * 60 * 24 * 30;
		private static readonly DateTime UnixEpoch = new DateTime(1971, 1, 1);

		public static uint GetExpiration(TimeSpan? validFor, DateTime? expiresAt)
		{
			if (validFor != null && expiresAt != null)
				throw new ArgumentException("You cannot specify both validFor and expiresAt.");

			if (expiresAt != null)
			{
				DateTime dt = expiresAt.Value;

				if (dt < UnixEpoch)
					throw new ArgumentOutOfRangeException("expiresAt", "expiresAt must be >= 1970/1/1");

				return (uint)(dt.ToUniversalTime() - UnixEpoch).TotalSeconds;
			}

			TimeSpan ts = validFor.Value;

			if (ts.TotalSeconds >= MaxSeconds || ts <= TimeSpan.Zero)
				throw new ArgumentOutOfRangeException("validFor", "validFor must be < 30 days && >= 0");

			return (uint)ts.TotalSeconds;
		}

		#region [ IDisposable                  ]
		void IDisposable.Dispose()
		{
			this.Dispose();
		}

		/// <summary>
		/// Releases all resources allocated by this instance
		/// </summary>
		/// <remarks>Technically it's not really neccesary to call this, since the client does not create "really" disposable objects, so it's safe to assume that when 
		/// the AppPool shuts down all resources will be released correctly and no handles or such will remain in the memory.</remarks>
		public void Dispose()
		{
			if (this.protocol == null)
				throw new ObjectDisposedException("MemcachedClient");

			GC.SuppressFinalize(this);

			try
			{
				this.protocol.Dispose();
			}
			finally
			{
				this.protocol = null;
			}
		}
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