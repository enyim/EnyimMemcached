using Enyim.Caching.Configuration;
using Enyim.Caching.Memcached;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Enyim.Caching.Tests
{
    public class MemcachedClientWithKeyTransformerTests : MemcachedClientTestsBase
    {
        public MemcachedClientWithKeyTransformerTests()
            : base(options => options.KeyTransformer = "Enyim.Caching.Memcached.TigerHashKeyTransformer")
        {
        }

        [Fact]
        public void When_Removing_A_Valid_Transformed_Key_Is_Successful()
        {
            var key = GetUniqueKey("remove");
            var storeResult = Store(key: key);
            StoreAssertPass(storeResult);

            var removeResult = _client.ExecuteRemove(key);
            Assert.True(removeResult.Success, "Success was false");
            Assert.True((removeResult.StatusCode ?? 0) == 0, "StatusCode was neither null nor 0");

            var getResult = _client.ExecuteGet(key);
            GetAssertFail(getResult);
        }


        [Fact]
        public async Task When_Removing_A_Valid_Transformed_Key_Is_Successful_Async()
        {
            var key = GetUniqueKey("remove");
            var storeResult = await StoreAsync(key: key);
            Assert.True(storeResult, "Success was false");

            var removeResult = await _client.RemoveAsync(key);

            Assert.True(removeResult, "Success was false");

            var getResult = await _client.GetAsync<object>(key);
            GetAssertFail(getResult);
        }

        [Fact]
        public void When_Getting_Existing_Item_Value_With_Transformed_Key_Is_Not_Null_And_Result_Is_Successful()
        {
            var key = GetUniqueKey("get");
            var value = GetRandomString();
            var storeResult = Store(key: key, value: value);
            StoreAssertPass(storeResult);

            var getResult = _client.ExecuteGet(key);
            GetAssertPass(getResult, value);
        }

        [Fact]
        public void When_Storing_Item_With_With_Transformed_Key_And_Valid_Cas_Result_Is_Successful()
        {
            var key = GetUniqueKey("cas");
            var value = GetRandomString();
            var storeResult = Store(StoreMode.Add, key, value);
            StoreAssertPass(storeResult);

            var casResult = _client.ExecuteCas(StoreMode.Set, key, value, storeResult.Cas);
            StoreAssertPass(casResult);
        }
    }
}
