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

		public StatsOperation(ServerPool pool) : base(pool) { }

		protected override bool ExecuteAction()
		{
			Dictionary<IPEndPoint, Dictionary<string, string>> retval = new Dictionary<IPEndPoint, Dictionary<string, string>>();

			BinaryRequest request = new BinaryRequest(OpCode.Stat);
			IList<ArraySegment<byte>> requestData = request.CreateBuffer();

			foreach (MemcachedNode server in this.ServerPool.WorkingServers)
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

							string key = Encoding.ASCII.GetString(data.Array, data.Offset, response.KeyLength);
							string value = Encoding.ASCII.GetString(data.Array, data.Offset + response.KeyLength, data.Count - response.KeyLength);
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