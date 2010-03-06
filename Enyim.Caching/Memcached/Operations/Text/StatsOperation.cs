using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace Enyim.Caching.Memcached.Operations.Text
{
	internal sealed class StatsOperation : Operation
	{
		private log4net.ILog log = log4net.LogManager.GetLogger(typeof(StatsOperation));

		private ServerStats results;

		public StatsOperation(ServerPool pool) : base(pool) { }

		public ServerStats Results
		{
			get { return this.results; }
		}

		protected override bool ExecuteAction()
		{
			Dictionary<IPEndPoint, Dictionary<string, string>> retval = new Dictionary<IPEndPoint, Dictionary<string, string>>();

			foreach (MemcachedNode server in this.ServerPool.WorkingServers)
			{
				using (PooledSocket socket = server.Acquire())
				{
					if (socket == null)
						continue;

					try
					{
						socket.SendCommand("stats");

						Dictionary<string, string> serverData = new Dictionary<string, string>(StringComparer.Ordinal);

						while (true)
						{
							string line = TextSocketHelper.ReadResponse(socket);

							// stat values are terminated by END
							if (String.Compare(line, "END", StringComparison.Ordinal) == 0)
								break;

							// expected response is STAT item_name item_value
							if (line.Length < 6 || String.Compare(line, 0, "STAT ", 0, 5, StringComparison.Ordinal) != 0)
							{
								if (log.IsWarnEnabled)
									log.Warn("Unknow response: " + line);

								continue;
							}

							// get the key&value
							string[] parts = line.Remove(0, 5).Split(' ');
							if (parts.Length != 2)
							{
								if (log.IsWarnEnabled)
									log.Warn("Unknow response: " + line);

								continue;
							}

							// store the stat item
							serverData[parts[0]] = parts[1];
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