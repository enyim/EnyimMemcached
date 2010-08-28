using System;
using System.Collections.Generic;
using System.Text;
using Enyim.Caching;
using Enyim.Caching.Memcached;
using System.Net;
using Enyim.Caching.Configuration;
using NorthScale.Store;
using NorthScale.Store.Configuration;
using System.Threading;

namespace DemoApp
{
	class Program
	{
		static void Main(string[] args)
		{
			var ns = new NorthScaleClient();

			var v1 = ns.CasStore(StoreMode.Set, "A", "1");
			var v2 = ns.GetWithCas("A");

			var v3 = ns.CasStore(StoreMode.Set, "A", "2", 1);
			var v4 = ns.GetWithCas("A");

			var v5 = ns.CasStore(StoreMode.Set, "A", "3", v1.Cas);
			var v6 = ns.GetWithCas("A");



			//ns.Store(StoreMode.Set, "K", 1);
			//var K = ns.Get2("K");

			//var prefix = new Random().Next(1000) + "_random_multi_get_key_";
			//var keys = new List<string>();

			//Console.WriteLine(ns.Increment("VALUE", 100, 2));
			//Console.WriteLine(ns.Increment("VALUE", 10, 24));

			//ns.FlushAll();
			////Thread.Sleep(10000);
			//Console.WriteLine("flush done");

			//for (var i = 0; i < 2000; i++)
			//{
			//    var key = prefix + i;
			//    keys.Add(key);

			//    ns.Store(StoreMode.Set, key, i);
			//}

			//var retval = ns.Get(keys);
			//Console.WriteLine(retval.Count);
			//Console.WriteLine(retval.Count == keys.Count ? "success" : "fail");

			Console.ReadLine();

			return;

			//new NorthScaleClient().Stats();

			//var nsc = new NorthScaleClientConfiguration();

			//nsc.KeyTransformer = new TigerHashKeyTransformer();
			//nsc.NodeLocator = new VBucketNodeLocator("crc", new VBucket[] { new VBucket(0, null), new VBucket(1, null) });

			//nsc.Urls.Add(new Uri("http://192.168.2.200:8080/pools/default"));

			//new NorthScaleClient(nsc);

			// create a MemcachedClient
			// in your application you can cache the client in a static variable or just recreate it every time
			// MemcachedClient mc = new MemcachedClient();

			// you can create another client using a different section from your app/web.config
			// this client instance can have different pool settings, key transformer, etc.
			// MemcachedClient mc2 = new MemcachedClient("memcached");

			// or just initialize the client from code

			MemcachedClientConfiguration config = new MemcachedClientConfiguration();
			config.Servers.Add(new IPEndPoint(IPAddress.Loopback, 11211));
			config.Protocol = MemcachedProtocol.Binary;
			config.Authentication.Type = typeof(PlainTextAuthenticator);
			config.Authentication.Parameters["userName"] = "demo";
			config.Authentication.Parameters["password"] = "demo";

			var mc = new MemcachedClient(config);

			for (var i = 0; i < 100; i++)
				mc.Store(StoreMode.Set, "Hello", "World");


			// simple multiget; please note that only 1.2.4 supports it (windows version is at 1.2.1)
			//List<string> keys = new List<string>();

			//for (int i = 1; i < 100; i++)
			//{
			//    string k = "aaaa" + i + "--" + (i * 2);
			//    keys.Add(k);

			//    mc.Store(StoreMode.Set, k, i);
			//}

			//IDictionary<string, ulong> cas;
			//IDictionary<string, object> retvals = mc.Get(keys, out cas);

			//List<string> keys2 = new List<string>(keys);
			//keys2.RemoveRange(0, 50);

			//IDictionary<string, object> retvals2 = mc.Get(keys2, out cas);
			//retvals2 = mc.Get(keys2, out cas);

			//ServerStats ms = mc.Stats();

			// store a string in the cache
			//mc.Store(StoreMode.Set, "MyKey", "Hello World");

			// retrieve the item from the cache
			//Console.WriteLine(mc.Get("MyKey"));

			//Console.WriteLine(mc.Increment("num1", 1, 10));
			//Console.WriteLine(mc.Increment("num1", 1, 10));
			//Console.WriteLine(mc.Decrement("num1", 1, 14));

			//// store some other items
			//mc.Store(StoreMode.Set, "D1", 1234L);
			//mc.Store(StoreMode.Set, "D2", DateTime.Now);
			//mc.Store(StoreMode.Set, "D3", true);
			//mc.Store(StoreMode.Set, "D4", new Product());

			//mc.Store(StoreMode.Set, "D5", new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });


			////mc2.Store(StoreMode.Set, "D1", 1234L);
			////mc2.Store(StoreMode.Set, "D2", DateTime.Now);
			////mc2.Store(StoreMode.Set, "D3", true);
			////mc2.Store(StoreMode.Set, "D4", new Product());

			//Console.WriteLine("D1: {0}", mc.Get("D1"));
			//Console.WriteLine("D2: {0}", mc.Get("D2"));
			//Console.WriteLine("D3: {0}", mc.Get("D3"));
			//Console.WriteLine("D4: {0}", mc.Get("D4"));

			//byte[] tmp = mc.Get<byte[]>("D5");

			//// delete them from the cache
			//mc.Remove("D1");
			//mc.Remove("D2");
			//mc.Remove("D3");
			//mc.Remove("D4");

			//ServerStats stats = mc.Stats();
			//Console.WriteLine(stats.GetValue(ServerStats.All, StatItem.ConnectionCount));
			//Console.WriteLine(stats.GetValue(ServerStats.All, StatItem.GetCount));

			//// add an item which is valid for 10 mins
			//mc.Store(StoreMode.Set, "D4", new Product(), new TimeSpan(0, 10, 0));

			Console.ReadLine();
		}

		// objects must be serializable to be able to store them in the cache
		[Serializable]
		class Product
		{
			public double Price = 1.24;
			public string Name = "Mineral Water";

			public override string ToString()
			{
				return String.Format("Product {{{0}: {1}}}", this.Name, this.Price);
			}
		}
	}
}
