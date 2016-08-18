using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace MemcachedTest
{
	public static class TestSetup
	{
		private static readonly object InitLock = new Object();

		private static int RefCount;
		private static List<IDisposable> Servers;

		public static void Run()
		{
			lock (InitLock)
			{
				if (Servers == null)
				{
					Servers = new List<IDisposable>
					{
						MemcachedServer.Run(11211),
						MemcachedServer.Run(11212)
					};
				}

				Interlocked.Increment(ref RefCount);
			}
		}

		public static void Cleanup()
		{
			lock (InitLock)
			{
				if (Interlocked.Decrement(ref RefCount) == 0)
				{
					foreach (var d in Servers)
						d.Dispose();

					Servers = null;
				}
			}
		}
	}
}

#region [ License information          ]

/* ************************************************************
 *
 *    Copyright (c) Attila Kiskó, enyim.com
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
