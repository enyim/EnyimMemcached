using System;

namespace Enyim.Caching.Memcached.Operations.Binary
{
	internal class MutatorOperation : ItemOperation
	{
		private ulong defaultValue;
		private ulong delta;
		private uint expires;
		private MutationMode mode;

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
			var extra = new byte[20];

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
			var socket = this.Socket;
			if (socket == null) return false;

			var request = new BinaryRequest(this.mode == MutationMode.Increment ? OpCode.Increment : OpCode.Decrement) { Key = this.HashedKey };
			this.UpdateExtra(request);

			request.Write(this.Socket);

			var response = new BinaryResponse();
			response.Read(this.Socket);

			var retval = response.Success;
			if (retval)
			{
				var data = response.Data;
				if (data.Count != 8)
					// temp hack to handle "Non-numeric server-side value for incr or decr" 
					return false; // throw new InvalidOperationException("result must be 8 bytes, received: " + data.Count);

				this.Result = BinaryConverter.DecodeUInt64(data.Array, data.Offset);
			}

			return retval;
		}

		public ulong Result { get; private set; }
	}
}
