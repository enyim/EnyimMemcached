using System;
using System.Net;

namespace Enyim.Caching.Memcached
{
	public interface IMemcachedNode : IDisposable
	{
		IPEndPoint EndPoint { get; }
		bool IsAlive { get; }
		bool Ping();
		PooledSocket Acquire();
	}
}
