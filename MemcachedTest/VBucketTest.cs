using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Enyim.Caching.Memcached;
using Enyim.Caching.Configuration;
using System.Net;

namespace MemcachedTest
{
	class VBucketTest
	{
		// copied from libvbucket
		Dictionary<string, int> keyToVBucket = new Dictionary<string, int>
        {
            { "hello", 0 },
            { "doctor", 0 },
            { "name", 3 },
            { "continue", 3 },
            { "yesterday", 0 },
            { "tomorrow", 1 },
            { "another key", 2 }
        };

		// copied from libvbucket
		VBucket[] buckets = new[]
                {
                    new VBucket(0, new [] { 1, 2 }),
                    new VBucket(1, new [] { 2, 0 }),
                    new VBucket(2, new [] { 1, -1 }),
                    new VBucket(1, new [] { 2, 0 }),
                };

		[TestCase]
		public void TestBuckets()
		{
			var vb = new VBucketNodeLocator("crc", buckets);

			var servers = new[] { "127.0.0.1", "127.0.0.2", "127.0.0.3" };
			var nodes = from s in servers
						let ip = IPAddress.Parse(s)
						select (IMemcachedNode)new MockNode(new IPEndPoint(ip, 11211));

			((IMemcachedNodeLocator)vb).Initialize(nodes.ToList());

			foreach (var kvp in keyToVBucket)
			{
				var b = vb.GetVBucket(kvp.Key);
				var index = Array.IndexOf(buckets, b);

				Assert.IsTrue(index == kvp.Value, "Key '" + kvp.Key + "': expected " + kvp.Value + " but found " + index);
			}
		}
	}
}
