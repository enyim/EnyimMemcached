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
				if (!server.IsAlive) continue;

				if (request == null)
				{
					BinaryRequest bq = new BinaryRequest(OpCode.FlushQ);
					request = bq.CreateBuffer();
				}

				using (PooledSocket socket = server.Acquire())
				{
					if (socket != null)						socket.Write(request);
				}
			}

			return true;
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
