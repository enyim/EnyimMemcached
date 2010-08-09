using System;
using System.Globalization;
using System.Text;

namespace Enyim.Caching.Memcached.Operations.Text
{
	internal class StoreOperation : ItemOperation
	{
		private static readonly ArraySegment<byte> DataTerminator = new ArraySegment<byte>(new byte[2] { (byte)'\r', (byte)'\n' });
		private StoreCommand mode;
		private object value;
		private uint expires;

		internal StoreOperation(IServerPool pool, StoreCommand mode, string key, object value, uint expires)
			: base(pool, key)
		{
			this.mode = mode;
			this.value = value;
			this.expires = expires;
		}

		protected override bool ExecuteAction()
		{
			PooledSocket socket = this.Socket;
			if (socket == null || !socket.IsAlive)
				return false;

			CacheItem item = this.ServerPool.Transcoder.Serialize(this.value);

			ushort flag = item.Flags;
			ArraySegment<byte> data = item.Data;

			// todo adjust the size to fit a request using a fnv hashed key
			StringBuilder sb = new StringBuilder(128);

			switch (mode)
			{
				case StoreCommand.Add:
					sb.Append("add ");
					break;
				case StoreCommand.Replace:
					sb.Append("replace ");
					break;
				case StoreCommand.Set:
					sb.Append("set ");
					break;

				case StoreCommand.Append:
					sb.Append("append ");
					break;

				case StoreCommand.Prepend:
					sb.Append("prepend ");
					break;

				case StoreCommand.CheckAndSet:
					sb.Append("cas ");
					break;

				default:
					throw new MemcachedClientException(mode + " is not supported.");
			}

			sb.Append(this.HashedKey);
			sb.Append(" ");
			sb.Append(flag.ToString(CultureInfo.InvariantCulture));
			sb.Append(" ");
			sb.Append(expires.ToString(CultureInfo.InvariantCulture));
			sb.Append(" ");
			sb.Append(Convert.ToString(data.Count - data.Offset, CultureInfo.InvariantCulture));

			if (mode == StoreCommand.CheckAndSet)
			{
				sb.Append(" ");
				sb.Append(Convert.ToString(this.Cas, CultureInfo.InvariantCulture));
			}

			ArraySegment<byte> commandBuffer = TextSocketHelper.GetCommandBuffer(sb.ToString());

			socket.Write(new ArraySegment<byte>[] { commandBuffer, data, StoreOperation.DataTerminator });

			return String.Compare(TextSocketHelper.ReadResponse(socket), "STORED", StringComparison.Ordinal) == 0;
		}

		protected override System.Collections.Generic.IList<ArraySegment<byte>> GetBuffer()
		{
			throw new NotImplementedException();
		}

		protected override bool ReadResponse(PooledSocket socket)
		{
			throw new NotImplementedException();
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
