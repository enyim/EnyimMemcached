using Enyim.Caching;
using NUnit.Framework;
using Enyim.Caching.Memcached;

namespace MemcachedTest
{
	[TestFixture]
	public class TextMemcachedClientTest : MemcachedClientTest
	{
		protected override MemcachedClient GetClient()
		{
			MemcachedClient client = new MemcachedClient("test/textConfig");
			client.FlushAll();

			return client;
		}

		[TestCase]
		public void IncrementTest()
		{
			using (MemcachedClient client = GetClient())
			{
				Assert.IsTrue(client.Store(StoreMode.Set, "VALUE", "100"), "Initialization failed");

				Assert.AreEqual(102L, client.Increment("VALUE", 0, 2));
				Assert.AreEqual(112L, client.Increment("VALUE", 0, 10));
			}
		}

		[TestCase]
		public void DecrementTest()
		{
			using (MemcachedClient client = GetClient())
			{
				client.Store(StoreMode.Set, "VALUE", "100");

				Assert.AreEqual(98L, client.Decrement("VALUE", 0, 2));
				Assert.AreEqual(88L, client.Decrement("VALUE", 0, 10));
			}
		}

		[TestCase]
		public void CASTest()
		{
			using (MemcachedClient client = GetClient())
			{
				// store the item
				var r1 = client.Store(StoreMode.Set, "CasItem1", "foo");

				Assert.IsTrue(r1, "Initial set failed.");

				// get back the item and check the cas value (it should match the cas from the set)
				var r2 = client.GetWithCas<string>("CasItem1");

				Assert.AreEqual(r2.Result, "foo", "Invalid data returned; expected 'foo'.");
				Assert.AreNotEqual(0, r2.Cas, "No cas value was returned.");

				var r3 = client.Cas(StoreMode.Set, "CasItem1", "bar", r2.Cas + 1001);

				Assert.IsFalse(r3.Result, "Overwriting with 'bar' should have failed.");

				var r4 = client.Cas(StoreMode.Set, "CasItem1", "baz", r2.Cas);

				Assert.IsTrue(r4.Result, "Overwriting with 'baz' should have succeeded.");

				var r5 = client.GetWithCas<string>("CasItem1");
				Assert.AreEqual(r5.Result, "baz", "Invalid data returned; excpected 'baz'.");
			}
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
