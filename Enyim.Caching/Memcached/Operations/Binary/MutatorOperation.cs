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

		public MutatorOperation(ServerPool pool, MutationMode mode, string key, ulong defaultValue, ulong delta, uint expires)
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