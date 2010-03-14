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