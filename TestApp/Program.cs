using System;
using System.Collections.Generic;
using System.Text;
using Enyim.Caching;
using Enyim.Caching.Memcached;
using System.Net;
using Enyim.Caching.Configuration;
using Membase;
using Membase.Configuration;
using System.Threading;

namespace DemoApp
{
	class Program
	{
		static void Main(string[] args)
		{
			log4net.Config.XmlConfigurator.Configure();

			var mcc = new MemcachedClientConfiguration();
			mcc.AddServer("192.168.2.200:11211");
			mcc.AddServer("192.168.2.202:11211");

			mcc.SocketPool.ReceiveTimeout = new TimeSpan(0, 0, 4);
			mcc.SocketPool.ConnectionTimeout = new TimeSpan(0, 0, 4);
			mcc.SocketPool.DeadTimeout = new TimeSpan(0, 0, 10);

			StressTest(new MemcachedClient(mcc));

			return;


			// or just initialize the client from code
			var nscc = new MembaseClientConfiguration();

			nscc.SocketPool.ReceiveTimeout = new TimeSpan(0, 0, 2);
			nscc.SocketPool.DeadTimeout = new TimeSpan(0, 0, 10);

			nscc.Urls.Add(new Uri("http://192.168.2.200:8080/pools/default"));
			nscc.Urls.Add(new Uri("http://192.168.2.202:8080/pools/default"));
			//nscc.Credentials = new NetworkCredential("A", "11111111");
			//nscc.BucketPassword = "pass";

			StressTest(new MembaseClient(nscc, "default"));

			return;

			var nc = new MembaseClient(nscc, "content");

			var stats1 = nc.Stats("slabs");
			foreach (var kvp in stats1.GetRaw("curr_connections"))
				Console.WriteLine("{0} -> {1}", kvp.Key, kvp.Value);

			var nc2 = new MembaseClient(nscc, "content");

			var stats2 = nc2.Stats();
			foreach (var kvp in stats2.GetRaw("curr_connections"))
				Console.WriteLine("{0} -> {1}", kvp.Key, kvp.Value);
		}

		private static void StressTest(MemcachedClient client)
		{
			var i = 0;
			var last = true;

			var progress = @"-\|/".ToCharArray();
			Console.CursorVisible = false;
			Dictionary<bool, int> counters = new Dictionary<bool, int>() { { true, 0 }, { false, 0 } };

			while (true)
			{
				var key = "Test_Key_" + i;
				var state = client.Store(StoreMode.Set, key, i) & client.Get<int>(key) == i;

				Action updateTitle = () => Console.Title = "Success: " + counters[true] + " Fail: " + counters[false];

				if (state != last)
				{
					Console.ForegroundColor = state ? ConsoleColor.White : ConsoleColor.Red;
					Console.Write(".");

					counters[state] = 0;
					last = state;

					updateTitle();
				}
				else if (i % 200 == 0)
				{
					Console.ForegroundColor = state ? ConsoleColor.White : ConsoleColor.Red;

					Console.Write(progress[(i / 200) % 4]);
					if (Console.CursorLeft == 0)
					{
						Console.CursorLeft = Console.WindowWidth - 1;
						Console.CursorTop -= 1;
					}
					else
					{
						Console.CursorLeft -= 1;
					}

					updateTitle();
				}

				i++;
				counters[state] = counters[state] + 1;
			}
		}
	}
}
