using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enyim.Caching.Memcached;

namespace Membase
{
	public interface ITouchOperation : ISingleItemOperation { }
	public interface IGetAndTouchOperation : IGetOperation { }
}
