using System;
using System.Collections.Generic;
using System.Text;

namespace Enyim.Caching.Memcached.Operations.Binary
{
	internal class ConcatenationOperation : ItemOperation
	{
		private ArraySegment<byte> data;
		private ConcatenationMode mode;

		public ConcatenationOperation(ServerPool pool, ConcatenationMode mode, string key, ArraySegment<byte> data)
			: base(pool, key)
		{
			this.data = data;
			this.mode = mode;
		}

		protected override bool ExecuteAction()
		{
			PooledSocket socket = this.Socket;
			if (socket == null) return false;

			BinaryRequest request = new BinaryRequest(this.mode == ConcatenationMode.Append ? OpCode.Append : OpCode.Prepend);
			request.Key = this.Key;
			request.Data = this.data;

			request.Write(socket);

			BinaryResponse response = new BinaryResponse();
			return response.Read(socket);
		}
	}
}
