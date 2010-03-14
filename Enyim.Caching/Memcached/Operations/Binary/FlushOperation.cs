using System;
using System.Collections.Generic;

namespace Enyim.Caching.Memcached.Operations.Binary
{
	internal class FlushOperation : Operation
	{
		public FlushOperation(IServerPool pool) : base(pool) { }

		protected override bool ExecuteAction()
		{
			IList<ArraySegment<byte>> request = null;

			foreach (IMemcachedNode server in this.ServerPool.GetServers())
			{
				if (request == null)
				{
					BinaryRequest bq = new BinaryRequest(OpCode.FlushQ);
					request = bq.CreateBuffer();
				}

				using (PooledSocket socket = server.Acquire())
				{
					if (socket != null)
						socket.Write(request);
				}
			}

			return true;
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