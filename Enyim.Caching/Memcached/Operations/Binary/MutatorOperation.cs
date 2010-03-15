using System;

namespace Enyim.Caching.Memcached.Operations.Binary
{
	internal class MutatorOperation : ItemOperation
	{
		private ulong defaultValue;
		private ulong delta;
		private uint expires;
		private MutationMode mode;
		private ulong result;

		public MutatorOperation(IServerPool pool, MutationMode mode, string key, ulong defaultValue, ulong delta, uint expires)
			: base(pool, key)
		{
			if (delta < 0) throw new ArgumentOutOfRangeException("delta", "delta must be >= 1");

			this.defaultValue = defaultValue;
			this.delta = delta;
			this.expires = expires;
			this.mode = mode;
		}

		private unsafe void UpdateExtra(BinaryRequest request)
		{
			byte[] extra = new byte[20];

			fixed (byte* buffer = extra)
			{
				BinaryConverter.EncodeUInt64(this.delta, buffer, 0);

				BinaryConverter.EncodeUInt64(this.defaultValue, buffer, 8);
				BinaryConverter.EncodeUInt32(this.expires, buffer, 16);
			}

			request.Extra = new ArraySegment<byte>(extra);
		}


		protected override bool ExecuteAction()
		{
			PooledSocket socket = this.Socket;
			if (socket == null) return false;

			BinaryRequest request = new BinaryRequest(this.mode == MutationMode.Increment ? OpCode.Increment : OpCode.Decrement);
			request.Key = this.HashedKey;

			this.UpdateExtra(request);

			request.Write(socket);

			BinaryResponse response = new BinaryResponse();
			bool retval = response.Read(socket);
			if (retval)
			{
				ArraySegment<byte> data = response.Data;
				if (data.Count != 8)
					throw new InvalidOperationException("result must be 8 bytes, received: " + data.Count);

				this.result = BinaryConverter.DecodeUInt64(data.Array, data.Offset);
			}

			return retval;
		}

		public ulong Result
		{
			get { return this.result; }
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
