using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Enyim.Caching;
using Enyim.Caching.Configuration;
using Enyim.Caching.Memcached;

namespace DemoApp
{
	class Program
	{
		static void Main(string[] args)
		{
			log4net.Config.XmlConfigurator.Configure();


			var mbcc = new MembaseClientConfiguration();

			mbcc.SocketPool.ReceiveTimeout = new TimeSpan(0, 0, 2);
			mbcc.SocketPool.DeadTimeout = new TimeSpan(0, 0, 10);

			mbcc.Urls.Add(new Uri("http://localhost:8091/pools/default"));

			var client = new MembaseClient(mbcc);

			var item1 = client.Cas(StoreMode.Set, "item1", 1);
			var item2 = client.Cas(StoreMode.Set, "item2", 2);

			var add1 = client.Cas(StoreMode.Add, "item1", 4);

			Console.WriteLine(add1.Result);
			Console.WriteLine(add1.Cas);
			Console.WriteLine(add1.StatusCode);

			Console.WriteLine("item1 = " + item1.Cas);
			Console.WriteLine("item2 = " + item2.Cas);

			Console.WriteLine("Go?");
			Console.ReadLine();

			var mre = new ManualResetEvent(false);

			ThreadPool.QueueUserWorkItem(o =>
			{
				mre.WaitOne();

				Console.WriteLine("Waiting before change 1");
				Thread.Sleep(4000);

				Console.WriteLine("item1 overwrite = " + client.Cas(StoreMode.Set, "item1", 4, item1.Cas).Result);

				Console.WriteLine("Waiting before change 2");
				Thread.Sleep(4000);

				Console.WriteLine("item2 overwrite = " + client.Cas(StoreMode.Set, "item2", 4, item2.Cas).Result);
			});

			//mre.Set();

			//client.Sync("item1", item1.Cas, SyncMode.Mutation);
			client.Sync(SyncMode.Mutation, new[] { new KeyValuePair<string, ulong>("item1", item1.Cas), new KeyValuePair<string, ulong>("item2", item2.Cas) });
			Console.WriteLine("Changed");

			Console.ReadLine();

			////nscc.PerformanceMonitorFactory = new Membase.Configuration.DefaultPerformanceMonitorFactory();
			////nscc.BucketPassword = "pass";

			////ThreadPool.QueueUserWorkItem(o => StressTest(new MembaseClient(nscc), "TesT_A_"));
			////ThreadPool.QueueUserWorkItem(o => StressTest(new MembaseClient("content", "content"), "TesT_B_"));

			////ThreadPool.QueueUserWorkItem(o => StressTest(new MembaseClient(nscc, "default"), "TesT_B_"));
			////ThreadPool.QueueUserWorkItem(o => StressTest(new MembaseClient(nscc, "default"), "TesT_C_"));
			////ThreadPool.QueueUserWorkItem(o => StressTest(new MembaseClient(nscc, "default"), "TesT_D_"));

			//Console.ReadLine();

			//return;

			//var mcc = new MemcachedClientConfiguration();
			//mcc.AddServer("192.168.2.200:11211");
			//mcc.AddServer("192.168.2.202:11211");

			//mcc.SocketPool.ReceiveTimeout = new TimeSpan(0, 0, 4);
			//mcc.SocketPool.ConnectionTimeout = new TimeSpan(0, 0, 4);
			//mcc.SocketPool.DeadTimeout = new TimeSpan(0, 0, 10);

			//StressTest(new MemcachedClient(mcc), "TesT_");

			//return;


			//var nc = new MembaseClient(nscc, "content", "content");

			//var stats1 = nc.Stats("slabs");
			//foreach (var kvp in stats1.GetRaw("curr_connections"))
			//    Console.WriteLine("{0} -> {1}", kvp.Key, kvp.Value);

			//var nc2 = new MembaseClient(nscc, "content", "content");

			//var stats2 = nc2.Stats();
			//foreach (var kvp in stats2.GetRaw("curr_connections"))
			//    Console.WriteLine("{0} -> {1}", kvp.Key, kvp.Value);
		}

		private static void MultigetSpeedTest()
		{
			//Enyim.Caching.LogManager.AssignFactory(new ConsoleLogFactory());
			var tmc = new MemcachedClientConfiguration();

			tmc.AddServer("172.16.203.2:11211");
			tmc.AddServer("172.16.203.2:11212");
			//tmc.AddServer("172.16.203.2:11213");
			//tmc.AddServer("172.16.203.2:11214");

			tmc.Protocol = MemcachedProtocol.Binary;
			//tmc.SocketPool.MinPoolSize = 1;
			//tmc.SocketPool.MaxPoolSize = 1;

			tmc.SocketPool.ReceiveTimeout = new TimeSpan(0, 0, 4);
			tmc.SocketPool.ConnectionTimeout = new TimeSpan(0, 0, 4);

			tmc.KeyTransformer = new DefaultKeyTransformer();

			var tc = new MemcachedClient(tmc);
			const string KeyPrefix = "asdfghjkl";

			var val = new byte[10 * 1024];
			val[val.Length - 1] = 1;

			for (var i = 0; i < 100; i++)
				if (!tc.Store(StoreMode.Set, KeyPrefix + i, val))
					Console.WriteLine("Fail " + KeyPrefix + i);

			var keys = Enumerable.Range(0, 500).Select(k => KeyPrefix + k).ToArray();

			Console.WriteLine("+");

			var sw = Stopwatch.StartNew();
			//tc.Get(KeyPrefix + "4");
			//sw.Stop();
			//Console.WriteLine(sw.ElapsedMilliseconds);

			//sw = Stopwatch.StartNew();

			//var p = tc.Get2(keys);

			//sw.Stop();
			//Console.WriteLine(sw.ElapsedMilliseconds);


			//sw = Stopwatch.StartNew();

			//var t = tc.Get(keys);
			//Console.WriteLine(" --" + t.Count);

			//sw.Stop();
			//Console.WriteLine(sw.ElapsedMilliseconds);

			//Console.WriteLine("Waiting");
			//Console.ReadLine();

			//return;

			for (var i = 0; i < 100; i++)
			{
				const int MAX = 300;

				sw = Stopwatch.StartNew();
				for (var j = 0; j < MAX; j++) tc.Get(keys);
				sw.Stop();
				Console.WriteLine(sw.ElapsedMilliseconds);

				//sw = Stopwatch.StartNew();
				//for (var j = 0; j < MAX; j++) tc.GetOld(keys);
				//sw.Stop();
				//Console.WriteLine(sw.ElapsedMilliseconds);

				//sw = Stopwatch.StartNew();
				//for (var j = 0; j < MAX; j++)
				//    foreach (var k in keys) tc.Get(k);
				//sw.Stop();
				//Console.WriteLine(sw.ElapsedMilliseconds);

				Console.WriteLine("----");
			}

			Console.ReadLine();
			return;
		}

		private static void StressTest(MemcachedClient client, string keyPrefix)
		{
			var i = 0;
			var last = true;

			var progress = @"-\|/".ToCharArray();
			Console.CursorVisible = false;
			Dictionary<bool, int> counters = new Dictionary<bool, int>() { { true, 0 }, { false, 0 } };

			while (true)
			{
				var key = keyPrefix + i;
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
					//Console.ForegroundColor = state ? ConsoleColor.White : ConsoleColor.Red;

					//Console.Write(progress[(i / 200) % 4]);
					//if (Console.CursorLeft == 0)
					//{
					//    Console.CursorLeft = Console.WindowWidth - 1;
					//    Console.CursorTop -= 1;
					//}
					//else
					//{
					//    Console.CursorLeft -= 1;
					//}

					updateTitle();
				}

				i++;
				counters[state] = counters[state] + 1;
			}
		}
	}
}
