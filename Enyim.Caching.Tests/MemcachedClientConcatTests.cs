using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Enyim.Caching.Tests
{
	[TestFixture]
	public class MemcachedClientConcatTests : MemcachedClientTestsBase
	{
		[Test]
		public void When_Appending_To_Existing_Value_Result_Is_Successful()
		{
			var key = GetUniqueKey("concat");
			var value = GetRandomString();

			var storeResult = Store(key: key);
			StoreAssertPass(storeResult);

			var toAppend = "The End";
			var data = new ArraySegment<byte>(Encoding.ASCII.GetBytes(toAppend));
			var concatResult = _Client.ExecuteAppend(key, data);
			ConcatAssertPass(concatResult);

			var getResult = _Client.ExecuteGet(key);
			GetAssertPass(getResult, value + toAppend);

		}

		[Test]
		public void When_Appending_To_Invalid_Key_Result_Is_Not_Successful()
		{
			var key = GetUniqueKey("concat");

			var toAppend = "The End";
			var data = new ArraySegment<byte>(Encoding.ASCII.GetBytes(toAppend));
			var concatResult = _Client.ExecuteAppend(key, data);
			ConcatAssertFail(concatResult);

			var getResult = _Client.ExecuteGet(key);
			GetAssertFail(getResult);

		}

		[Test]
		public void When_Prepending_To_Existing_Value_Result_Is_Successful()
		{
			var key = GetUniqueKey("concat");
			var value = GetRandomString();

			var storeResult = Store(key: key);
			StoreAssertPass(storeResult);

			var toPrepend = "The Beginning";
			var data = new ArraySegment<byte>(Encoding.ASCII.GetBytes(toPrepend));
			var concatResult = _Client.ExecutePrepend(key, data);
			ConcatAssertPass(concatResult);

			var getResult = _Client.ExecuteGet(key);
			GetAssertPass(getResult, toPrepend + value);

		}

		[Test]
		public void When_Prepending_To_Invalid_Key_Result_Is_Not_Successful()
		{
			var key = GetUniqueKey("concat");

			var toPrepend = "The End";
			var data = new ArraySegment<byte>(Encoding.ASCII.GetBytes(toPrepend));
			var concatResult = _Client.ExecutePrepend(key, data);
			ConcatAssertFail(concatResult);

			var getResult = _Client.ExecuteGet(key);
			GetAssertFail(getResult);

		}

		[Test]
		public void When_Appending_To_Existing_Value_Result_Is_Successful_With_Valid_Cas()
		{
			var key = GetUniqueKey("concat");
			var value = GetRandomString();

			var storeResult = Store(key: key);
			StoreAssertPass(storeResult);

			var toAppend = "The End";
			var data = new ArraySegment<byte>(Encoding.ASCII.GetBytes(toAppend));
			var concatResult = _Client.ExecuteAppend(key, storeResult.Cas, data);
			ConcatAssertPass(concatResult);

			var getResult = _Client.ExecuteGet(key);
			GetAssertPass(getResult, value + toAppend);

		}

		[Test]
		public void When_Appending_To_Existing_Value_Result_Is_Not_Successful_With_Invalid_Cas()
		{
			var key = GetUniqueKey("concat");
			var value = GetRandomString();

			var storeResult = Store(key: key);
			StoreAssertPass(storeResult);

			var toAppend = "The End";
			var data = new ArraySegment<byte>(Encoding.ASCII.GetBytes(toAppend));
			var concatResult = _Client.ExecuteAppend(key, storeResult.Cas + (2 << 28), data);
			ConcatAssertFail(concatResult);
		}

		[Test]
		public void When_Prepending_To_Existing_Value_Result_Is_Successful_With_Valid_Cas()
		{
			var key = GetUniqueKey("concat");
			var value = GetRandomString();

			var storeResult = Store(key: key);
			StoreAssertPass(storeResult);

			var tpPrepend = "The Beginning";
			var data = new ArraySegment<byte>(Encoding.ASCII.GetBytes(tpPrepend));
			var concatResult = _Client.ExecuteAppend(key, storeResult.Cas, data);
			ConcatAssertPass(concatResult);

			var getResult = _Client.ExecuteGet(key);
			GetAssertPass(getResult, value + tpPrepend);

		}

		[Test]
		public void When_Prepending_To_Existing_Value_Result_Is_Not_Successful_With_Invalid_Cas()
		{
			var key = GetUniqueKey("concat");
			var value = GetRandomString();

			var storeResult = Store(key: key);
			StoreAssertPass(storeResult);

			var tpPrepend = "The Beginning";
			var data = new ArraySegment<byte>(Encoding.ASCII.GetBytes(tpPrepend));
			var concatResult = _Client.ExecuteAppend(key, storeResult.Cas - 1, data);
			ConcatAssertFail(concatResult);

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
