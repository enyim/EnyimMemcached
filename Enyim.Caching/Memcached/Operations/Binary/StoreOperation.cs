using System;

namespace Enyim.Caching.Memcached.Operations.Binary
{
	internal class StoreOperation : ItemOperation
	{

		private StoreCommand mode;
		private object value;
		private uint expires;

		public StoreOperation(IServerPool pool, StoreCommand mode, string key, object value, uint expires) :
			base(pool, key)
		{
			this.mode = mode;
			this.value = value;
			this.expires = expires;
		}

		protected override bool ExecuteAction()
		{
			PooledSocket socket = this.Socket;
			if (socket == null) return false;

			OpCode op;
			switch (this.mode)
			{
				case StoreCommand.Add: op = OpCode.Add; break;
				case StoreCommand.Set: op = OpCode.Set; break;
				case StoreCommand.Replace: op = OpCode.Replace; break;
				default: throw new ArgumentOutOfRangeException("mode", mode + " is not supported");
			}

			BinaryRequest request = new BinaryRequest(op);
			byte[] extra = new byte[8];

			CacheItem item = this.ServerPool.Transcoder.Serialize(this.value);

			BinaryConverter.EncodeUInt32((uint)item.Flags, extra, 0);
			BinaryConverter.EncodeUInt32(expires, extra, 4);

			request.Extra = new ArraySegment<byte>(extra);
			request.Data = item.Data;
			request.Key = this.HashedKey;

			request.Write(socket);

			// TEST
			// no response means success for the quiet commands
			// if (socket.Available == 0) return true;

			BinaryResponse response = new BinaryResponse();

			return response.Read(socket);
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