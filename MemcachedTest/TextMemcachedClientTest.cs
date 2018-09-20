using Enyim.Caching;
using Enyim.Caching.Memcached;
using System;
using Xunit;

namespace MemcachedTest
{
	public class TextMemcachedClientTest : MemcachedClientTest
	{

		[Fact]
		public void IncrementTest()
		{
			using (MemcachedClient client = GetClient(MemcachedProtocol.Text))
			{               
                Assert.True(client.Store(StoreMode.Set, "VALUE", "100"), "Initialization failed");

				Assert.Equal((ulong)102, client.Increment("VALUE", 0, 2));
				Assert.Equal((ulong)112, client.Increment("VALUE", 0, 10));
			}
		}

		[Fact]
		public void DecrementTest()
		{
			using (MemcachedClient client = GetClient(MemcachedProtocol.Text))
			{
				client.Store(StoreMode.Set, "VALUE", "100");

				Assert.Equal((ulong)98, client.Decrement("VALUE", 0, 2));
				Assert.Equal((ulong)88, client.Decrement("VALUE", 0, 10));
			}
		}

		[Fact]
		public void CASTest()
		{
			using (MemcachedClient client = GetClient())
			{
				// store the item
				var r1 = client.Store(StoreMode.Set, "CasItem1", "foo");

				Assert.True(r1, "Initial set failed.");

				// get back the item and check the cas value (it should match the cas from the set)
				var r2 = client.GetWithCas<string>("CasItem1");

				Assert.Equal("foo", r2.Result);
				Assert.NotEqual((ulong)0, r2.Cas);

				var r3 = client.Cas(StoreMode.Set, "CasItem1", "bar", r2.Cas - 1);

				Assert.False(r3.Result, "Overwriting with 'bar' should have failed.");

				var r4 = client.Cas(StoreMode.Set, "CasItem1", "baz", r2.Cas);

				Assert.True(r4.Result, "Overwriting with 'baz' should have succeeded.");

				var r5 = client.GetWithCas<string>("CasItem1");
				Assert.Equal("baz", r5.Result);
			}
		}


        [Fact]
        public void StoreWithTimeSpan()
        {
            using (MemcachedClient client = GetClient(MemcachedProtocol.Text))
            {
                var key = "abc";
                var value = "core memcache write";
                bool success = client.Store(Enyim.Caching.Memcached.StoreMode.Set, key, value, new TimeSpan(0, 10, 0));
                Assert.True(success);
                Assert.Equal(value, client.Get<string>(key));
            }
        }
    }
}

#region [ License information          ]
/* ************************************************************
 * 
 *    Copyright (c) 2010 Attila Kisk? enyim.com
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
