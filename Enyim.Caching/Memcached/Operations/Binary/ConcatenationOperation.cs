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