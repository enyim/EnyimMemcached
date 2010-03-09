using System;

namespace Enyim.Caching.Memcached.Operations.Text
{
	/// <summary>
	/// Memcached client.
	/// </summary>
	internal sealed class TextProtocol : IProtocolImplementation
	{
		private IServerPool pool;

		public TextProtocol(IServerPool pool)
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

		bool IProtocolImplementation.Store(StoreMode mode, string key, object value, uint expires)
		{
			if (value == null) return false;

			using (StoreOperation s = new StoreOperation(pool, (StoreCommand)mode, key, value, expires))
			{
				s.Cas = 0;


				return s.Execute();
			}
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
			if (mode == MutationMode.Increment)
			{
				IncrementOperation op = new IncrementOperation(this.pool, key, delta);
			
				return op.Execute() ? 0 : op.Result;
			}
			else
			{
				DecrementOperation op = new DecrementOperation(this.pool, key, delta);

				return op.Execute() ? 0 : op.Result;
			}
		}

		bool IProtocolImplementation.Delete(string key)
		{
			using (DeleteOperation d = new DeleteOperation(this.pool, key))
			{
				return d.Execute();
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
			StoreCommand command = mode == ConcatenationMode.Append
									? StoreCommand.Append
									: StoreCommand.Prepend;

			using (StoreOperation so = new StoreOperation(this.pool, command, key, data, 0))
			{
				return so.Execute();
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
			return null;
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