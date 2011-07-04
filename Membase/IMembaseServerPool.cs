using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enyim.Caching.Memcached;

namespace Membase
{
	public interface IMembaseServerPool : IServerPool
	{
		new IMembaseOperationFactory OperationFactory { get; }
	}
}
