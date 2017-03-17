using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enyim.Caching.Configuration;
using Enyim.Caching.Memcached.Results;
using Enyim.Caching.Memcached;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace Enyim.Caching.Tests
{
    public abstract class MemcachedClientTestsBase
    {
        protected MemcachedClient _client;

        public MemcachedClientTestsBase()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddEnyimMemcached(options => options.AddServer("memcached", 11211));
            services.AddLogging();
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            _client = serviceProvider.GetService<IMemcachedClient>() as MemcachedClient;
        }

        protected string GetUniqueKey(string prefix = null)
        {
            return (!string.IsNullOrEmpty(prefix) ? prefix + "_" : "") +
                "unit_test_" + DateTime.Now.Ticks + "_" + Guid.NewGuid();
        }

        protected IEnumerable<string> GetUniqueKeys(string prefix = null, int max = 5)
        {

            var keys = new List<string>(max);
            for (int i = 0; i < max; i++)
            {
                keys.Add(GetUniqueKey(prefix));
            }

            return keys;
        }

        protected string GetRandomString()
        {
            var rand = new Random((int)DateTime.Now.Ticks).Next();
            return "unit_test_value_" + rand;
        }

        protected IStoreOperationResult Store(StoreMode mode = StoreMode.Set, string key = null, object value = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                key = GetUniqueKey("store");
            }

            if (value == null)
            {
                value = GetRandomString();
            }
            return _client.ExecuteStore(mode, key, value);
        }

        protected void StoreAssertPass(IStoreOperationResult result)
        {
            Assert.True(result.Success, "Success was false");
            Assert.True(result.Cas > 0, "Cas value was 0");
            Assert.Equal(0, result.StatusCode);
        }

        protected void StoreAssertFail(IStoreOperationResult result)
        {
            Assert.False(result.Success, "Success was true");
            Assert.Equal((ulong)0, result.Cas);
            Assert.True(result.StatusCode > 0, "StatusCode not greater than 0");
            Assert.NotNull(result.InnerResult);
        }

        protected void GetAssertPass(IGetOperationResult result, object expectedValue)
        {
            Assert.True(result.Success, "Success was false");
            Assert.True(result.Cas > 0, "Cas value was 0");
            Assert.True((result.StatusCode ?? 0) == 0, "StatusCode was neither 0 nor null");
            Assert.Equal(expectedValue, result.Value);
        }

        protected void GetAssertFail(IGetOperationResult result)
        {
            Assert.False(result.Success, "Success was true");
            Assert.Equal((ulong)0, result.Cas);
            Assert.True(result.StatusCode > 0, "StatusCode not greater than 0");
            Assert.False(result.HasValue, "HasValue was true");
            Assert.Null(result.Value);
        }

        protected void MutateAssertPass(IMutateOperationResult result, ulong expectedValue)
        {
            Assert.True(result.Success, "Success was false");
            Assert.Equal(expectedValue, result.Value);
            Assert.True(result.Cas > 0, "Cas was not greater than 0");
            Assert.True((result.StatusCode ?? 0) == 0, "StatusCode was not null or 0");
        }

        protected void MutateAssertFail(IMutateOperationResult result)
        {
            Assert.False(result.Success, "Success was true");
            Assert.Equal((ulong)0, result.Cas);
            Assert.True((result.StatusCode ?? 1) != 0, "StatusCode was 0");
        }

        protected void ConcatAssertPass(IConcatOperationResult result)
        {
            Assert.True(result.Success, "Success was false");
            Assert.True(result.Cas > 0, "Cas value was 0");
            Assert.Equal(0, result.StatusCode);
        }

        protected void ConcatAssertFail(IConcatOperationResult result)
        {
            Assert.False(result.Success, "Success was true");
            Assert.Equal((ulong)0, result.Cas);
            Assert.True((result.StatusCode ?? 1) > 0, "StatusCode not greater than 0");
        }
    }
}

#region [ License information          ]
/* ************************************************************
 * 
 *    @author Couchbase <info@couchbase.com>
 *    @copyright 2012 Couchbase, Inc.
 *    @copyright 2012 Attila Kiskó, enyim.com
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