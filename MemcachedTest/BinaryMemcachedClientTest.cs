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
	}
}
