using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Enyim.Caching
{
	public class NLogFactory : Enyim.Caching.ILogFactory
	{
		ILog ILogFactory.GetLogger(string name)
		{
			var log = NLog.LogManager.GetLogger(name);

			return new NLogWrapper(log);
		}

		ILog ILogFactory.GetLogger(Type type)
		{
			var log = NLog.LogManager.GetLogger(type.FullName);

			return new NLogWrapper(log);
		}
	}
}
