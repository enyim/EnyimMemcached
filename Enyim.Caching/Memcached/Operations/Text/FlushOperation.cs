using System;
using System.Collections.Generic;
using System.Text;

namespace Enyim.Caching.Memcached.Operations.Text
{
	internal sealed class FlushOperation : Operation
	{
		public FlushOperation(ServerPool pool) : base(pool) { }

		protected override bool ExecuteAction()
		{
			foreach (MemcachedNode server in this.ServerPool.WorkingServers)
			{
				using (PooledSocket socket = server.Acquire())
				{
					if (socket != null)
					{
						TextSocketHelper.SendCommand(socket, "flush_all");
						TextSocketHelper.ReadResponse(socket); // No-op the response to avoid data hanging around.
					}
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