using System;
using System.Collections.Generic;

namespace Enyim.Caching.Memcached.Operations.Binary
{
	internal class FlushOperation : Operation
	{
		public FlushOperation(ServerPool pool) : base(pool) { }

		protected override bool ExecuteAction()
		{
			IList<ArraySegment<byte>> request = null;

			foreach (MemcachedNode server in this.ServerPool.WorkingServers)
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
