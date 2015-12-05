using System;
using System.Text;
using Enyim.Caching.Memcached.Results;
using Enyim.Caching.Memcached.Results.Helpers;
using Enyim.Caching.Memcached.Results.Extensions;

namespace Enyim.Caching.Memcached.Protocol.Binary
{
	public class StoreOperation : BinarySingleItemOperation, IStoreOperation
	{
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

		protected override BinaryRequest Build()
		{
			OpCode op;
			switch (this.mode)
			{
				case StoreMode.Add: op = OpCode.Add; break;
				case StoreMode.Set: op = OpCode.Set; break;
				case StoreMode.Replace: op = OpCode.Replace; break;
				default: throw new ArgumentOutOfRangeException("mode", mode + " is not supported");
			}

			var extra = new byte[8];

			BinaryConverter.EncodeUInt32((uint)this.value.Flags, extra, 0);
			BinaryConverter.EncodeUInt32(expires, extra, 4);

			var request = new BinaryRequest(op)
			{
				Key = this.Key,
				Cas = this.Cas,
				Extra = new ArraySegment<byte>(extra),
				Data = this.value.Data
			};

			return request;
		}

		protected override IOperationResult ProcessResponse(BinaryResponse response)
		{
			var result = new BinaryOperationResult();

			this.StatusCode = response.StatusCode;
			if (response.StatusCode == 0)
			{
				return result.Pass();
			}
			else
			{
				var message = ResultHelper.ProcessResponseData(response.Data);
				return result.Fail(message);
			}
		}

		StoreMode IStoreOperation.Mode
		{
			get { return this.mode; }
		}

		protected internal override bool ReadResponseAsync(PooledSocket socket, System.Action<bool> next)
		{
			throw new System.NotSupportedException();
		}
	}
}

#region [ License information          ]
/* ************************************************************
 * 
 *    Copyright (c) 2010 Attila Kisk? enyim.com
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
