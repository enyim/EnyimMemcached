using System;
using System.Collections.Generic;
using System.Linq;
using Enyim.Caching.Memcached;
using NUnit.Framework;

namespace Enyim.Caching.Tests
{
	[TestFixture]
	public class SingleNodeLocatorTest
	{
		[TestCase]
		public void TestInitializationWithEmptyList()
		{
			var locator = (IMemcachedNodeLocator)new SingleNodeLocator();
			locator.Initialize(new List<IMemcachedNode>());

			Assert.IsNull(locator.Locate("key"));
			Assert.AreEqual(Enumerable.Empty<IMemcachedNode>(), locator.GetWorkingNodes());
		}
	}
}
