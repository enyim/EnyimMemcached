using System;
using System.Collections.Generic;

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


		~TextProtocol()
		{
			try { ((IDisposable)this).Dispose(); }
			catch { }
		}

		/// <summary>
		/// Releases all resources allocated by this instance
		/// </summary>
		/// <remarks>Technically it's not really neccesary to call this, since the client does not create "really" disposable objects, so it's safe to assume that when 
		/// the AppPool shuts down all resources will be released correctly and no handles or such will remain in the memory.</remarks>
		public void Dispose()
		{
			if (this.pool != null)
			{
				((IDisposable)this.pool).Dispose();
				this.pool = null;
			}
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

		IDictionary<string, object> IProtocolImplementation.Get(System.Collections.Generic.IEnumerable<string> keys)
		{
			using (MultiGetOperation mgo = new MultiGetOperation(this.pool, keys))
			{
				return mgo.Execute() ? mgo.Result : new Dictionary<string, object>();
			}
		}

		ulong IProtocolImplementation.Mutate(MutationMode mode, string key, ulong defaultValue, ulong delta, uint expiration)
		{
			if (expiration != 0)
				throw new NotSupportedException("Mutators with expiration are not supported by the text protocol.");

			if (mode == MutationMode.Increment)
			{
				IncrementOperation op = new IncrementOperation(this.pool, key, delta);

				return op.Execute() ? op.Result : 0;
			}
			else
			{
				DecrementOperation op = new DecrementOperation(this.pool, key, delta);

				return op.Execute() ? op.Result : 0;
			}
		}

		bool IProtocolImplementation.Remove(string key)
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
