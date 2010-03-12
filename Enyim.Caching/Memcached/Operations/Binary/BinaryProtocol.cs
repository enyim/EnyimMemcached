using System;
using System.Collections.Generic;

namespace Enyim.Caching.Memcached.Operations.Binary
{
	/// <summary>
	/// Memcached client.
	/// </summary>
	internal class BinaryProtocol : IProtocolImplementation
	{
		private IServerPool pool;

		public BinaryProtocol(IServerPool pool)
		{
			this.pool = pool;
		}

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
			if (this.pool == null)
				throw new ObjectDisposedException("MemcachedClient");

			((IDisposable)this.pool).Dispose();
			this.pool = null;
		}

		private bool TryGet(string key, out object value)
		{
			using (GetOperation g = new GetOperation(this.pool, key))
			{
				bool retval = g.Execute();
				value = retval ? g.Result : null;

				return retval;
			}
		}

		bool IProtocolImplementation.Store(StoreMode mode, string key, object value, uint expires)
		{
			// TODO allow nulls?
			if (value == null) return false;

			using (StoreOperation s = new StoreOperation(pool, (StoreCommand)mode, key, value, expires))
			{
				return s.Execute();
			}
		}

		bool IProtocolImplementation.TryGet(string key, out object value)
		{
			return TryGet(key, out value);
		}

		object IProtocolImplementation.Get(string key)
		{
			object retval;

			TryGet(key, out retval);

			return retval;
		}

		ulong IProtocolImplementation.Mutate(MutationMode mode, string key, ulong defaultValue, ulong delta, uint expiration)
		{
			using (MutatorOperation m = new MutatorOperation(this.pool, mode, key, defaultValue, delta, expiration))
			{
				return m.Execute() ? m.Result : m.Result;
			}
		}

		bool IProtocolImplementation.Remove(string key)
		{
			using (DeleteOperation g = new DeleteOperation(this.pool, key))
			{
				return g.Execute();
			}
		}

		void IProtocolImplementation.FlushAll()
		{
			using (FlushOperation f = new FlushOperation(this.pool))
			{
				f.Execute();
			}
		}

		bool IProtocolImplementation.Concatenate(ConcatenationMode mode, string key, ArraySegment<byte> data)
		{
			using (ConcatenationOperation co = new ConcatenationOperation(this.pool, mode, key, data))
			{
				return co.Execute();
			}
		}

		ServerStats IProtocolImplementation.Stats()
		{
			using (StatsOperation so = new StatsOperation(this.pool))
			{
				so.Execute();

				return so.Results;
			}
		}

		IAuthenticator IProtocolImplementation.CreateAuthenticator(ISaslAuthenticationProvider provider)
		{
			return new BinaryAuthenticator(provider);
		}

		#region IProtocolImplementation Members


		System.Collections.Generic.IDictionary<string, object> IProtocolImplementation.Get(System.Collections.Generic.IEnumerable<string> keys)
		{
			using (var mg = new MultiGetOperation(this.pool, keys))
			{
				return mg.Execute() ? mg.Result : new Dictionary<string, object>();
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