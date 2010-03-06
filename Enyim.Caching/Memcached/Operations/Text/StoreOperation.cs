using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Security.Cryptography;
using System.Globalization;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO.Compression;

namespace Enyim.Caching.Memcached.Operations.Text
{
	internal class StoreOperation : ItemOperation
	{
		private const int MaxSeconds = 60 * 60 * 24 * 30;

		private static readonly ArraySegment<byte> DataTerminator = new ArraySegment<byte>(new byte[2] { (byte)'\r', (byte)'\n' });
		private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1);

		private StoreCommand mode;
		private object value;

		private long expires;
		private ulong casValue;

		internal StoreOperation(ServerPool pool, StoreCommand mode, string key, object value, ulong casValue, TimeSpan validFor, DateTime expiresAt)
			: base(pool, key)
		{
			this.mode = mode;
			this.value = value;
			this.casValue = casValue;

			this.expires = GetExpiration(validFor, expiresAt);
		}

		private static long GetExpiration(TimeSpan validFor, DateTime expiresAt)
		{
			if (validFor >= TimeSpan.Zero && expiresAt > DateTime.MinValue)
				throw new ArgumentException("You cannot specify both validFor and expiresAt.");

			if (expiresAt > DateTime.MinValue)
			{
				if (expiresAt < UnixEpoch)
					throw new ArgumentOutOfRangeException("expiresAt", "expiresAt must be >= 1970/1/1");

				return (long)(expiresAt.ToUniversalTime() - UnixEpoch).TotalSeconds;
			}

			if (validFor.TotalSeconds >= MaxSeconds || validFor < TimeSpan.Zero)
				throw new ArgumentOutOfRangeException("validFor", "validFor must be < 30 days && >= 0");

			return (long)validFor.TotalSeconds;
		}

		protected override bool ExecuteAction()
		{
			if (this.Socket == null)
				return false;

			CacheItem item = this.ServerPool.Transcoder.Serialize(this.value);

			return this.Store(item.Flag, item.Data);
		}

		private bool Store(ushort flag, ArraySegment<byte> data)
		{
			StringBuilder sb = new StringBuilder(100);

			switch (this.mode)
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
			sb.Append(this.expires.ToString(CultureInfo.InvariantCulture));
			sb.Append(" ");
			sb.Append(Convert.ToString(data.Count - data.Offset, CultureInfo.InvariantCulture));

			if (mode == StoreCommand.CheckAndSet)
			{
				sb.Append(" ");
				sb.Append(Convert.ToString(this.casValue, CultureInfo.InvariantCulture));
			}

			ArraySegment<byte> commandBuffer = PooledSocket.GetCommandBuffer(sb.ToString());

			this.Socket.Write(new ArraySegment<byte>[] { commandBuffer, data, StoreOperation.DataTerminator });

			bool retval = String.Compare(TextSocketHelper.ReadResponse(this.Socket), "STORED", StringComparison.Ordinal) == 0;
			this.Socket.OwnerNode.PerfomanceCounters.LogStore(this.mode, retval);

			return retval;
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