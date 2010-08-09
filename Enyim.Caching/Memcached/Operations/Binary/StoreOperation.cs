using System;
using System.Text;

namespace Enyim.Caching.Memcached.Operations.Binary
{
	internal class StoreOperation : ItemOperation2, IStoreOperation
	{
		private static log4net.ILog log = log4net.LogManager.GetLogger(typeof(StatsOperation));

		private StoreMode mode;
		private CacheItem value;
		private uint expires;

		public StoreOperation(StoreMode mode, string key, CacheItem value, uint expires) :
			base(key)
		{
			this.mode = mode;
			this.value = value;
			this.expires = expires;
		}

		protected override System.Collections.Generic.IList<ArraySegment<byte>> GetBuffer()
		{
			OpCode op;
			switch (this.mode)
			{
				case StoreMode.Add: op = OpCode.Add; break;
				case StoreMode.Set: op = OpCode.Set; break;
				case StoreMode.Replace: op = OpCode.Replace; break;
				default: throw new ArgumentOutOfRangeException("mode", mode + " is not supported");
			}

			var request = new BinaryRequest(op);
			var extra = new byte[8];

			BinaryConverter.EncodeUInt32((uint)this.value.Flags, extra, 0);
			BinaryConverter.EncodeUInt32(expires, extra, 4);

			request.Extra = new ArraySegment<byte>(extra);
			request.Data = this.value.Data;
			request.Key = this.Key;

			return request.CreateBuffer();
		}


		protected override bool ReadResponse(PooledSocket socket)
		{
			var response = new BinaryResponse();
			var retval = response.Read(socket);

			if (!retval)
				if (log.IsDebugEnabled)
					log.DebugFormat("Store failed for key '{0}'. Reason: {1}", this.Key, Encoding.ASCII.GetString(response.Data.Array, response.Data.Offset, response.Data.Count));

			return retval;
		}

		StoreMode IStoreOperation.Mode
		{
			get { return this.mode; }
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
