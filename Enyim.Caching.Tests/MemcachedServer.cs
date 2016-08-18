using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace MemcachedTest
{
	public static class MemcachedServer
	{
		static readonly string BasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools");
		static readonly string ExePath = Path.Combine(BasePath, "memcached.exe");

		public static IDisposable Run(int port = 11211, bool verbose = false, int maxMem = 512, bool hidden = true)
		{
			var args = $"-E default_engine.so -p {port} -m {maxMem}";
			if (verbose) args += " -vv";

			var process = Process.Start(new ProcessStartInfo
			{
				Arguments = args,
				FileName = ExePath,
				WorkingDirectory = BasePath,
				WindowStyle = hidden ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Normal
			});

			return new KillProcess(process);
		}

		#region [ KillProcess                  ]

		class KillProcess : IDisposable
		{
			private Process process;

			public KillProcess(Process process)
			{
				this.process = process;

				AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
				AppDomain.CurrentDomain.DomainUnload += CurrentDomain_ProcessExit;
			}

			~KillProcess()
			{
				GC.WaitForPendingFinalizers();

				Dispose();
			}

			private void CurrentDomain_ProcessExit(object sender, EventArgs e)
			{
				Dispose();
			}

			public void Dispose()
			{
				GC.SuppressFinalize(this);

				if (process != null)
				{
					using (process)
						process.Kill();

					process = null;
				}
			}
		}

		#endregion
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
