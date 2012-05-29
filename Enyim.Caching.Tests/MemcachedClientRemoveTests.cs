using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Enyim.Caching.Tests
{
	[TestFixture]
	public class MemcachedClientRemoveTests : MemcachedClientTestsBase
	{
		[Test]
		public void When_Removing_A_Valid_Key_Result_Is_Successful()
		{
			var key = GetUniqueKey("remove");
			var storeResult = Store(key: key);
			StoreAssertPass(storeResult);

			var removeResult = _Client.ExecuteRemove(key);
			Assert.That(removeResult.Success, Is.True, "Success was false");
			Assert.That(removeResult.StatusCode, Is.Null.Or.EqualTo(0), "StatusCode was neither null nor 0");

			var getResult = _Client.ExecuteGet(key);
			GetAssertFail(getResult);
		}

		[Test]
		public void When_Removing_An_Invalid_Key_Result_Is_Not_Successful()
		{
			var key = GetUniqueKey("remove");

			var removeResult = _Client.ExecuteRemove(key);
			Assert.That(removeResult.Success, Is.False, "Success was true");
		}
	}
}
