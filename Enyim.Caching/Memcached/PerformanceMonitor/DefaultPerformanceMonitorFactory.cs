using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Enyim.Caching.Memcached
{
	public class DefaultPerformanceMonitorFactory : IProviderFactory<IPerformanceMonitor>
	{
		private string name;

		internal DefaultPerformanceMonitorFactory() { }

		public DefaultPerformanceMonitorFactory(string name)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentException("Name must be specified.", "name");

			this.name = name;
		}

		void IProvider.Initialize(Dictionary<string, string> parameters)
		{
			if ((parameters != null
					&& (!parameters.TryGetValue("name", out this.name)
						|| String.IsNullOrEmpty(this.name)))
				|| (parameters == null && String.IsNullOrEmpty(this.name)))
				throw new ArgumentException("The DefaultPerformanceMonitor must have a name assigned. Use the name attribute in the configuration file.");
		}

		IPerformanceMonitor IProviderFactory<IPerformanceMonitor>.Create()
		{
			return new DefaultPerformanceMonitor(this.name);
		}
	}
}
