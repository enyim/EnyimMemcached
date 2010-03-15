using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace Enyim.Caching.Memcached.Operations.Binary
{
	internal class StatsOperation : Operation
	{
		private log4net.ILog log = log4net.LogManager.GetLogger(typeof(StatsOperation));

		private ServerStats results;

		public StatsOperation(IServerPool pool) : base(pool) { }

		protected override bool ExecuteAction()
		{
			Dictionary<IPEndPoint, Dictionary<string, string>> retval = new Dictionary<IPEndPoint, Dictionary<string, string>>();

			BinaryRequest request = new BinaryRequest(OpCode.Stat);
			IList<ArraySegment<byte>> requestData = request.CreateBuffer();

			foreach (IMemcachedNode server in this.ServerPool.GetServers())
			{
				using (PooledSocket socket = server.Acquire())
				{
					if (socket == null || !socket.IsAlive) continue;

					try
					{
						socket.Write(requestData);

						BinaryResponse response = new BinaryResponse();
						Dictionary<string, string> serverData = new Dictionary<string, string>(StringComparer.Ordinal);

						while (response.Read(socket) && response.KeyLength > 0)
						{
							ArraySegment<byte> data = response.Data;

							string key = BinaryConverter.DecodeKey(data.Array, data.Offset, response.KeyLength);
							string value = BinaryConverter.DecodeKey(data.Array, data.Offset + response.KeyLength, data.Count - response.KeyLength);
							serverData[key] = value;
						}

						retval[server.EndPoint] = serverData;
					}
					catch (Exception e)
					{
						log.Error(e);
					}
				}
			}

			this.results = new ServerStats(retval);

			return true;
		}

		public ServerStats Results
		{
			get { return this.results; }
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
