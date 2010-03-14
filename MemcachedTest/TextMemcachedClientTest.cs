using Enyim.Caching;
using NUnit.Framework;

namespace MemcachedTest
{
	[TestFixture]
	public class TextMemcachedClientTest : MemcachedClientTest
	{
		protected override MemcachedClient GetClient()
		{
			MemcachedClient client = new MemcachedClient("test/textConfig");
			client.FlushAll();

			return client;
		}
	}
}
