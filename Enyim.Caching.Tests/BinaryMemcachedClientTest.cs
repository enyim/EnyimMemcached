using Enyim.Caching;
using NUnit.Framework;
using Enyim.Caching.Memcached;

namespace MemcachedTest
{
	/// <summary>
	///This is a test class for Enyim.Caching.MemcachedClient and is intended
	///to contain all Enyim.Caching.MemcachedClient Unit Tests
	///</summary>
	[TestFixture]
	public class BinaryMemcachedClientTest : MemcachedClientTest
	{
		protected override MemcachedClient GetClient()
		{
			MemcachedClient client = new MemcachedClient("test/binaryConfig");
			client.FlushAll();

			return client;
		}

		[TestCase]
		public void IncrementTest()
		{
			using (MemcachedClient client = GetClient())
			{
				Assert.AreEqual(100, client.Increment("VALUE", 100, 2), "Non-exsiting value should be set to default");
				Assert.AreEqual(124, client.Increment("VALUE", 10, 24));
			}
		}

		[TestCase]
		public void DecrementTest()
		{
			using (MemcachedClient client = GetClient())
			{
				Assert.AreEqual(100, client.Decrement("VALUE", 100, 2), "Non-exsiting value should be set to default");
				Assert.AreEqual(76, client.Decrement("VALUE", 10, 24));

				Assert.AreEqual(0, client.Decrement("VALUE", 100, 1000), "Decrement should stop at 0");
			}
		}

		[TestCase]
		public void IncrementNoDefaultTest()
		{
			using (MemcachedClient client = GetClient())
			{
				Assert.IsNull(client.Get("VALUE"), "Initialization failed");

				Assert.AreEqual(2, client.Increment("VALUE", 2, 2), "Increment failed");

				var value = client.Get("VALUE");
				Assert.AreEqual("2", value, "Get failed. Expected 2, returned: '" + value + "'");
			}
		}

		[TestCase]
		public virtual void CASTest()
		{
			using (MemcachedClient client = GetClient())
			{
				// store the item
				var r1 = client.Cas(StoreMode.Set, "CasItem1", "foo");

				Assert.IsTrue(r1.Result, "Initial set failed.");
				Assert.AreNotEqual(r1.Cas, 0, "No cas value was returned.");

				// get back the item and check the cas value (it should match the cas from the set)
				var r2 = client.GetWithCas<string>("CasItem1");

				Assert.AreEqual(r2.Result, "foo", "Invalid data returned; expected 'foo'.");
				Assert.AreEqual(r1.Cas, r2.Cas, "Cas values do not match.");

				var r3 = client.Cas(StoreMode.Set, "CasItem1", "bar", r1.Cas + 1001);

				Assert.IsFalse(r3.Result, "Overwriting with 'bar' should have failed.");

				var r4 = client.Cas(StoreMode.Set, "CasItem1", "baz", r2.Cas);

				Assert.IsTrue(r4.Result, "Overwriting with 'baz' should have succeeded.");

				var r5 = client.GetWithCas<string>("CasItem1");
				Assert.AreEqual(r5.Result, "baz", "Invalid data returned; excpected 'baz'.");
			}
		}

		[TestCase]
		public void AppendCASTest()
		{
			using (MemcachedClient client = GetClient())
			{
				// store the item
				var r1 = client.Cas(StoreMode.Set, "CasAppend", "foo");

				Assert.IsTrue(r1.Result, "Initial set failed.");
				Assert.AreNotEqual(r1.Cas, 0, "No cas value was returned.");

				var r2 = client.Append("CasAppend", r1.Cas, new System.ArraySegment<byte>(new byte[] { (byte)'l' }));

				Assert.IsTrue(r2.Result, "Append should have succeeded.");

				// get back the item and check the cas value (it should match the cas from the set)
				var r3 = client.GetWithCas<string>("CasAppend");

				Assert.AreEqual(r3.Result, "fool", "Invalid data returned; expected 'fool'.");
				Assert.AreEqual(r2.Cas, r3.Cas, "Cas values r2:r3 do not match.");

				var r4 = client.Append("CasAppend", r1.Cas, new System.ArraySegment<byte>(new byte[] { (byte)'l' }));
				Assert.IsFalse(r4.Result, "Append with invalid CAS should have failed.");
			}
		}

		[TestCase]
		public void PrependCASTest()
		{
			using (MemcachedClient client = GetClient())
			{
				// store the item
				var r1 = client.Cas(StoreMode.Set, "CasPrepend", "ool");

				Assert.IsTrue(r1.Result, "Initial set failed.");
				Assert.AreNotEqual(r1.Cas, 0, "No cas value was returned.");

				var r2 = client.Prepend("CasPrepend", r1.Cas, new System.ArraySegment<byte>(new byte[] { (byte)'f' }));

				Assert.IsTrue(r2.Result, "Prepend should have succeeded.");

				// get back the item and check the cas value (it should match the cas from the set)
				var r3 = client.GetWithCas<string>("CasPrepend");

				Assert.AreEqual(r3.Result, "fool", "Invalid data returned; expected 'fool'.");
				Assert.AreEqual(r2.Cas, r3.Cas, "Cas values r2:r3 do not match.");

				var r4 = client.Prepend("CasPrepend", r1.Cas, new System.ArraySegment<byte>(new byte[] { (byte)'l' }));
				Assert.IsFalse(r4.Result, "Prepend with invalid CAS should have failed.");
			}
		}

		[TestCase]
		public void IncrementLongTest()
		{
			var initialValue = 56UL * (ulong)System.Math.Pow(10, 11) + 1234;

			using (MemcachedClient client = GetClient())
			{
				Assert.AreEqual(initialValue, client.Increment("VALUE", initialValue, 2UL), "Non-existing value should be set to default");
				Assert.AreEqual(initialValue + 24, client.Increment("VALUE", 10UL, 24UL));
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
