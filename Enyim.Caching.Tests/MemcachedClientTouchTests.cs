using System;
using NUnit.Framework;


namespace Enyim.Caching.Tests
{
	[TestFixture]
	public class MemcachedClientTouchTests : MemcachedClientTestsBase
	{
		[Test]
		public void When_Touching_Existing_Item_Result_Is_Successful()
		{
			var key = GetUniqueKey("get");
			var value = GetRandomString();
			var storeResult = Store(key: key, value: value);
			StoreAssertPass(storeResult);

			var touchResult = _Client.ExecuteTouch(key, new TimeSpan(0, 1, 0));
			TouchAssertPass(touchResult);
		}

		[Test]
		public void When_Touching_Nonexistent_Item_Result_Is_NotSuccessful()
		{
			var key = GetUniqueKey("get");
			var touchResult = _Client.ExecuteTouch(key, new TimeSpan(0, 1, 0));
			TouchAssertFail(touchResult);
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