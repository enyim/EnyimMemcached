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
