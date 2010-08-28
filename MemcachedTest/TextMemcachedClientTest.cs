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
