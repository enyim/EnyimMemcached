using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Enyim.Caching.Memcached;
using Enyim.Caching.Memcached.Results;

namespace Enyim.Caching.Tests
{

	[TestFixture(Description = "MemcachedClient Store Tests")]
	public class MemcachedClientCasTests : MemcachedClientTestsBase
	{

		[Test]
		public void When_Storing_Item_With_Valid_Cas_Result_Is_Successful()
		{
			var key = GetUniqueKey("cas");
			var value = GetRandomString();
			var storeResult = Store(StoreMode.Add, key, value);
			StoreAssertPass(storeResult);

			var casResult = _Client.ExecuteCas(StoreMode.Set, key, value, storeResult.Cas);
			StoreAssertPass(casResult);
		}

		[Test]
		public void When_Storing_Item_With_Invalid_Cas_Result_Is_Not_Successful()
		{
			var key = GetUniqueKey("cas");
			var value = GetRandomString();
			var storeResult = Store(StoreMode.Add, key, value);
			StoreAssertPass(storeResult);

			var casResult = _Client.ExecuteCas(StoreMode.Set, key, value, storeResult.Cas + (2 << 28));
			StoreAssertFail(casResult);
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
