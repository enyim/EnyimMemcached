using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enyim.Caching.Memcached;

namespace Membase
{
	public interface IMembaseOperationFactory : IOperationFactory
	{
		ITouchOperation Touch(string key, uint newExpiration);
		IGetAndTouchOperation GetAndTouch(string key, uint newExpiration);
		ISyncOperation Sync(SyncMode mode, KeyValuePair<string, ulong>[] keys, int replicationCount);
	}
}
