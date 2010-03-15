using Enyim.Caching;
using NUnit.Framework;

namespace MemcachedTest
{
	/// <summary>
	///This is a test class for Enyim.Caching.MemcachedClient and is intended
	///to contain all Enyim.Caching.MemcachedClient Unit Tests
	///</summary>
	[TestFixture]
	public class BinaryMemcachedClientTest : MemcachedClientTest
	{
		protected override MemcachedClient GetClient()
		{
			MemcachedClient client = new MemcachedClient("test/binaryConfig");
			client.FlushAll();

			return client;
		}

		[TestCase]
		public void IncrementTest()
		{
			using (MemcachedClient client = GetClient())
			{
				Assert.AreEqual(100, client.Increment("VALUE", 100, 2), "Non-exsiting value should be set to default");
				Assert.AreEqual(124, client.Increment("VALUE", 10, 24));
			}
		}

		[TestCase]
		public void DecrementTest()
		{
			using (MemcachedClient client = GetClient())
			{
				Assert.AreEqual(100, client.Decrement("VALUE", 100, 2), "Non-exsiting value should be set to default");
				Assert.AreEqual(76, client.Decrement("VALUE", 10, 24));

				Assert.AreEqual(0, client.Decrement("VALUE", 100, 1000), "Decrement should stop at 0");
			}
		}

	}
}
