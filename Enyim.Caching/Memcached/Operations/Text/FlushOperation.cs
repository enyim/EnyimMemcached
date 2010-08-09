
namespace Enyim.Caching.Memcached.Operations.Text
{
	internal sealed class FlushOperation : Operation
	{
		public FlushOperation(IServerPool pool) : base(pool) { }

		protected override bool ExecuteAction()
		{
			foreach (IMemcachedNode server in this.ServerPool.GetServers())
			{
				if (!server.IsAlive) continue;

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

		protected override System.Collections.Generic.IList<System.ArraySegment<byte>> GetBuffer()
		{
			throw new System.NotImplementedException();
		}

		protected override bool ReadResponse(PooledSocket socket)
		{
			throw new System.NotImplementedException();
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
