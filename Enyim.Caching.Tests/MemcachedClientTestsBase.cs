using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Enyim.Caching.Configuration;
using Enyim.Caching.Memcached.Results;
using Enyim.Caching.Memcached;

namespace Enyim.Caching.Tests
{
	[TestFixture]
	public abstract class MemcachedClientTestsBase
	{
		private Random random = new Random();
		protected MemcachedClient _Client;

		[SetUp]
		public void SetUp()
		{
			var config = new MemcachedClientConfiguration();
			config.AddServer("127.0.0.1", 11211);

			_Client = new MemcachedClient(config);
		}

		[TestFixtureSetUp]
		public void FixtureSetUp()
		{
			log4net.Config.XmlConfigurator.Configure();
			MemcachedTest.TestSetup.Run();
		}

		[TestFixtureTearDown]
		public void FixtureTearDown()
		{
			MemcachedTest.TestSetup.Cleanup();
		}

		protected string GetUniqueKey(string prefix = null)
		{
			return (String.IsNullOrEmpty(prefix) ? "" : prefix + "_")
					+ "unit_test_"
					+ DateTime.Now.Ticks
					+ "_" + random.Next();
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
			return _Client.ExecuteStore(mode, key, value);
		}

		protected void StoreAssertPass(IStoreOperationResult result)
		{
			Assert.That(result.Success, Is.True, "Success was false");
			Assert.That(result.Cas, Is.GreaterThan(0), "Cas value was 0");
			Assert.That(result.StatusCode, Is.EqualTo(0), "StatusCode was not 0");
		}

		protected void StoreAssertFail(IStoreOperationResult result)
		{
			Assert.That(result.Success, Is.False, "Success was true");
			Assert.That(result.Cas, Is.EqualTo(0), "Cas value was not 0");
			Assert.That(result.StatusCode, Is.GreaterThan(0), "StatusCode not greater than 0");
			Assert.That(result.InnerResult, Is.Not.Null, "InnerResult was null");
		}

		protected void GetAssertPass(IGetOperationResult result, object expectedValue)
		{
			Assert.That(result.Success, Is.True, "Success was false");
			Assert.That(result.Cas, Is.GreaterThan(0), "Cas value was 0");
			Assert.That(result.StatusCode, Is.EqualTo(0).Or.Null, "StatusCode was neither 0 nor null");
			Assert.That(result.Value, Is.EqualTo(expectedValue), "Actual value was not expected value: " + result.Value);
		}

		protected void GetAssertFail(IGetOperationResult result)
		{
			Assert.That(result.Success, Is.False, "Success was true");
			Assert.That(result.Cas, Is.EqualTo(0), "Cas value was not 0");
			Assert.That(result.StatusCode, Is.Null.Or.GreaterThan(0), "StatusCode not greater than 0");
			Assert.That(result.HasValue, Is.False, "HasValue was true");
			Assert.That(result.Value, Is.Null, "Value was not null");
		}

		protected void MutateAssertPass(IMutateOperationResult result, ulong expectedValue)
		{
			Assert.That(result.Success, Is.True, "Success was false");
			Assert.That(result.Value, Is.EqualTo(expectedValue), "Value was not expected value: " + expectedValue);
			Assert.That(result.Cas, Is.GreaterThan(0), "Cas was not greater than 0");
			Assert.That(result.StatusCode, Is.Null.Or.EqualTo(0), "StatusCode was not null or 0");
		}

		protected void MutateAssertFail(IMutateOperationResult result)
		{
			Assert.That(result.Success, Is.False, "Success was true");
			Assert.That(result.Cas, Is.EqualTo(0), "Cas 0");
			Assert.That(result.StatusCode, Is.Null.Or.Not.EqualTo(0), "StatusCode was 0");
		}

		protected void ConcatAssertPass(IConcatOperationResult result)
		{
			Assert.That(result.Success, Is.True, "Success was false");
			Assert.That(result.Cas, Is.GreaterThan(0), "Cas value was 0");
			Assert.That(result.StatusCode, Is.EqualTo(0), "StatusCode was not 0");
		}

		protected void ConcatAssertFail(IConcatOperationResult result)
		{
			Assert.That(result.Success, Is.False, "Success was true");
			Assert.That(result.Cas, Is.EqualTo(0), "Cas value was not 0");
			Assert.That(result.StatusCode, Is.Null.Or.GreaterThan(0), "StatusCode not greater than 0");
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
