using System;
using Enyim.Caching;
using Enyim.Caching.Memcached;
using Enyim.Caching.Memcached.Transcoders;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Linq;

namespace MemcachedTest
{
    public abstract class MemcachedClientTest
    {
        private static readonly Enyim.Caching.ILog log = Enyim.Caching.LogManager.GetLogger(typeof(MemcachedClientTest));
        public const string TestObjectKey = "Hello_World";

        protected virtual MemcachedClient GetClient(MemcachedProtocol protocol = MemcachedProtocol.Binary, bool useBinaryFormatterTranscoder = false)
        {
            IServiceCollection services = new ServiceCollection();
            services.AddEnyimMemcached(options =>
            {
                options.AddServer("memcached", 11211);
                options.Protocol = protocol;
                //if (useBinaryFormatterTranscoder)
                //{
                //    options.Transcoder = "BinaryFormatterTranscoder";
                //}
            });
            if(useBinaryFormatterTranscoder)
            {
                services.AddSingleton<ITranscoder,BinaryFormatterTranscoder>();
            }

            services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Information).AddConsole());

            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var client = serviceProvider.GetService<IMemcachedClient>() as MemcachedClient;
            client.Remove("VALUE");
            return client;
        }

        [Serializable]// This attribute is for BinaryFormatterTranscoder
        public class TestData
        {
            public TestData() { }

            public string FieldA;
            public string FieldB;
            public int FieldC;
            public bool FieldD;
        }

        /// <summary>
        ///A test for Store (StoreMode, string, byte[], int, int)
        ///</summary>
        [Fact]
        public async Task StoreObjectTest()
        {
            TestData td = new TestData();
            td.FieldA = "Hello";
            td.FieldB = "World";
            td.FieldC = 19810619;
            td.FieldD = true;

            using (MemcachedClient client = GetClient())
            {
                Assert.True(await client.StoreAsync(StoreMode.Set, TestObjectKey, td, DateTime.Now.AddSeconds(5)));
            }

            using (MemcachedClient client = GetClient(MemcachedProtocol.Binary, true))
            {
                Assert.True(await client.StoreAsync(StoreMode.Set, TestObjectKey, td, DateTime.Now.AddSeconds(5)));
            }
        }

        [Fact]
        public void GetObjectTest()
        {
            TestData td = new TestData();
            td.FieldA = "Hello";
            td.FieldB = "World";
            td.FieldC = 19810619;
            td.FieldD = true;

            using (MemcachedClient client = GetClient())
            {
                Assert.True(client.Store(StoreMode.Set, TestObjectKey, td), "Initialization failed.");

                TestData td2 = client.Get<TestData>(TestObjectKey);

                Assert.NotNull(td2);
                Assert.Equal("Hello", td2.FieldA);
                Assert.Equal("World", td2.FieldB);
                Assert.Equal(19810619, td2.FieldC);
                Assert.True(td2.FieldD, "Object was corrupted.");
            }

            using (MemcachedClient client = GetClient(MemcachedProtocol.Binary, true))
            {
                Assert.True(client.Store(StoreMode.Set, TestObjectKey, td), "Initialization failed.");
                TestData td2 = client.Get<TestData>(TestObjectKey);

                Assert.NotNull(td2);
                Assert.Equal("Hello", td2.FieldA);
                Assert.Equal("World", td2.FieldB);
                Assert.Equal(19810619, td2.FieldC);
                Assert.True(td2.FieldD, "Object was corrupted.");
            }
        }

        [Fact]
        public void DeleteObjectTest()
        {
            using (MemcachedClient client = GetClient())
            {
                TestData td = new TestData();
                Assert.True(client.Store(StoreMode.Set, TestObjectKey, td), "Initialization failed.");

                Assert.True(client.Remove(TestObjectKey), "Remove failed.");
                Assert.Null(client.Get(TestObjectKey));
            }
        }

        [Fact]
        public async Task StoreStringTest()
        {
            using (MemcachedClient client = GetClient())
            {
                Assert.True(await client.StoreAsync(StoreMode.Set, "TestString", "Hello world!", DateTime.Now.AddSeconds(10)), "StoreString failed.");

                Assert.Equal("Hello world!", await client.GetValueAsync<string>("TestString"));
            }
        }


        [Fact]
        public void StoreLongTest()
        {
            using (MemcachedClient client = GetClient())
            {
                Assert.True(client.Store(StoreMode.Set, "TestLong", 65432123456L), "StoreLong failed.");

                Assert.Equal(65432123456L, client.Get<long>("TestLong"));
            }
        }

        [Fact]
        public void StoreArrayTest()
        {
            byte[] bigBuffer = new byte[200 * 1024];

            for (int i = 0; i < bigBuffer.Length / 256; i++)
            {
                for (int j = 0; j < 256; j++)
                {
                    bigBuffer[i * 256 + j] = (byte)j;
                }
            }

            using (MemcachedClient client = GetClient())
            {
                Assert.True(client.Store(StoreMode.Set, "BigBuffer", bigBuffer), "StoreArray failed");

                byte[] bigBuffer2 = client.Get<byte[]>("BigBuffer");

                for (int i = 0; i < bigBuffer.Length / 256; i++)
                {
                    for (int j = 0; j < 256; j++)
                    {
                        if (bigBuffer2[i * 256 + j] != (byte)j)
                        {
                            Assert.Equal(j, bigBuffer[i * 256 + j]);
                            break;
                        }
                    }
                }
            }
        }

        [Fact]
        public async Task ExpirationTestTimeSpan()
        {
            using (MemcachedClient client = GetClient())
            {
                var cacheKey = $"ExpirationTest-TimeSpan-{new Random().Next()}";
                Assert.True(await client.StoreAsync(StoreMode.Set, cacheKey, "ExpirationTest:TimeSpan", new TimeSpan(0, 0, 3)), "Expires:Timespan failed");
                Assert.Equal("ExpirationTest:TimeSpan", await client.GetValueAsync<string>(cacheKey));

                await Task.Delay(TimeSpan.FromSeconds(4));

                Assert.Null(await client.GetValueAsync<string>(cacheKey));
            }
        }

        [Fact]
        public async Task ExpirationTestDateTime()
        {
            using (MemcachedClient client = GetClient())
            {
                var cacheKey = $"Expires-DateTime-{new Random().Next()}";
                DateTime expiresAt = DateTime.Now.AddSeconds(3);
                Assert.True(await client.StoreAsync(StoreMode.Set, cacheKey, "Expires:DateTime", expiresAt), "Expires:DateTime failed");
                Assert.Equal("Expires:DateTime", await client.GetValueAsync<string>(cacheKey));

                await Task.Delay(TimeSpan.FromSeconds(4));

                Assert.Null(await client.GetValueAsync<string>(cacheKey));
            }
        }

        [Fact]
        public void AddSetReplaceTest()
        {
            using (MemcachedClient client = GetClient())
            {
                log.Debug("Cache should be empty.");

                var cacheKey = $"{nameof(AddSetReplaceTest)}-{Guid.NewGuid()}";

                Assert.True(client.Store(StoreMode.Set, cacheKey, "1"), "Initialization failed");

                log.Debug("Setting VALUE to 1.");

                Assert.Equal("1", client.Get(cacheKey));

                log.Debug("Adding VALUE; this should return false.");
                Assert.False(client.Store(StoreMode.Add, cacheKey, "2"), "Add should have failed");

                log.Debug("Checking if VALUE is still '1'.");
                Assert.Equal("1", client.Get(cacheKey));

                log.Debug("Replacing VALUE; this should return true.");
                Assert.True(client.Store(StoreMode.Replace, cacheKey, "4"), "Replace failed");

                log.Debug("Checking if VALUE is '4' so it got replaced.");
                Assert.Equal("4", client.Get(cacheKey));

                log.Debug("Removing VALUE.");
                Assert.True(client.Remove(cacheKey), "Remove failed");

                log.Debug("Replacing VALUE; this should return false.");
                Assert.False(client.Store(StoreMode.Replace, cacheKey, "8"), "Replace should not have succeeded");

                log.Debug("Checking if VALUE is 'null' so it was not replaced.");
                Assert.Null(client.Get(cacheKey));

                log.Debug("Adding VALUE; this should return true.");
                Assert.True(client.Store(StoreMode.Add, cacheKey, "16"), "Item should have been Added");

                log.Debug("Checking if VALUE is '16' so it was added.");
                Assert.Equal("16", client.Get(cacheKey));

                log.Debug("Passed AddSetReplaceTest.");
            }
        }

        private string[] keyParts = { "multi", "get", "test", "key", "parts", "test", "values" };

        protected string MakeRandomKey(int partCount)
        {
            var sb = new StringBuilder();
            var rnd = new Random();

            for (var i = 0; i < partCount; i++)
            {
                sb.Append(keyParts[rnd.Next(keyParts.Length)]).Append(":");
            }

            sb.Length--;

            return sb.ToString();
        }

        [Fact]
        public async Task MultiGetTest()
        {
            using (var client = GetClient())
            {
                var keys = new List<string>();

                for (int i = 0; i < 10; i++)
                {
                    string k = $"Hello_Multi_Get_{Guid.NewGuid()}_" + i;
                    keys.Add(k);

                    Assert.True(await client.StoreAsync(StoreMode.Set, k, i, DateTime.Now.AddSeconds(30)), "Store of " + k + " failed");
                }

                IDictionary<string, int> retvals = await client.GetAsync<int>(keys);

                Assert.NotEmpty(retvals);
                Assert.Equal(keys.Count, retvals.Count);

                int value = 0;
                for (int i = 0; i < keys.Count; i++)
                {
                    string key = keys[i];

                    Assert.True(retvals.TryGetValue(key, out value), "missing key: " + key);
                    Assert.Equal(value, i);
                }

                var key1 = $"test_key1_{Guid.NewGuid()}";
                var key2 = $"test_key2_{Guid.NewGuid()}";
                var obj1 = new HashSet<int> { 1, 2 };
                var obj2 = new HashSet<int> { 3, 4 };
                await client.StoreAsync(StoreMode.Set, key1, obj1, DateTime.Now.AddSeconds(10));
                await client.StoreAsync(StoreMode.Set, key2, obj2, DateTime.Now.AddSeconds(10));

                var multiResult = await client.GetAsync<HashSet<int>>(new string[] { key1, key2 });
                Assert.Equal(2, multiResult.Count);
                Assert.Equal(obj1.First(), multiResult[key1].First());
                Assert.Equal(obj2.First(), multiResult[key2].First());
            }
        }

        [Fact]
        public virtual async Task MultiGetWithCasTest()
        {
            using (var client = GetClient())
            {
                var keys = new List<string>();
                var tasks = new List<Task<bool>>();

                for (int i = 0; i < 10; i++)
                {
                    string k = $"Hello_Multi_Get_{Guid.NewGuid()}_{new Random().Next()}" + i;
                    keys.Add(k);

                    tasks.Add(client.StoreAsync(StoreMode.Set, k, i, DateTime.Now.AddSeconds(300)));
                }

                await Task.WhenAll(tasks);

                foreach (var task in tasks)
                { 
                    Assert.True(await task, "Store failed");
                }

                var retvals = await client.GetWithCasAsync(keys);

                Assert.Equal(keys.Count, retvals.Count);

                tasks.Clear();
                for (int i = 0; i < keys.Count; i++)
                {
                    string key = keys[i];

                    Assert.True(retvals.TryGetValue(key, out var value), "missing key: " + key);
                    Assert.Equal(value.Result, i);
                    Assert.NotEqual(value.Cas, (ulong)0);

                    tasks.Add(client.RemoveAsync(key));
                }

                await Task.WhenAll(tasks);                
            }
        }

        [Fact]
        public void IncrementLongTest()
        {
            var initialValue = 56UL * (ulong)System.Math.Pow(10, 11) + 1234;

            using (MemcachedClient client = GetClient())
            {
                Assert.Equal(initialValue, client.Increment("VALUE", initialValue, 2UL));
                Assert.Equal(initialValue + 24, client.Increment("VALUE", 10UL, 24UL));
            }
        }

        [Fact]
        public async Task FlushTest()
        {
            using (MemcachedClient client = GetClient())
            {
                for (int i = 0; i < 10; i++)
                {
                    string cacheKey = $"Hello_Flush_{i}";
                    Assert.True(await client.StoreAsync(StoreMode.Set, cacheKey, i, DateTime.Now.AddSeconds(30)));
                }

                await client.FlushAllAsync();

                for (int i = 0; i < 10; i++)
                {
                    string cacheKey = $"Hello_Flush_{i}";
                    Assert.Null(await client.GetValueAsync<string>(cacheKey));
                }
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
