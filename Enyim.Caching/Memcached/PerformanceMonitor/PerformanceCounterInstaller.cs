using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Enyim.Caching.Memcached
{
	[System.ComponentModel.RunInstaller(true)]
	public class PerformanceCounterInstaller : System.Diagnostics.PerformanceCounterInstaller
	{
		public PerformanceCounterInstaller()
		{
			this.CategoryName = DefaultPerformanceMonitor.CategoryName;
			this.CategoryType = PerformanceCounterCategoryType.MultiInstance;
			this.UninstallAction = System.Configuration.Install.UninstallAction.Remove;

			// Get
			this.Counters.AddRange(DefaultPerformanceMonitor.OpMonitor.CreateCounters(DefaultPerformanceMonitor.Names.Get));
			this.Counters.AddRange(DefaultPerformanceMonitor.OpMonitor.CreateCounters(DefaultPerformanceMonitor.Names.Set));
			this.Counters.AddRange(DefaultPerformanceMonitor.OpMonitor.CreateCounters(DefaultPerformanceMonitor.Names.Add));
			this.Counters.AddRange(DefaultPerformanceMonitor.OpMonitor.CreateCounters(DefaultPerformanceMonitor.Names.Replace));
			this.Counters.AddRange(DefaultPerformanceMonitor.OpMonitor.CreateCounters(DefaultPerformanceMonitor.Names.Delete));
			this.Counters.AddRange(DefaultPerformanceMonitor.OpMonitor.CreateCounters(DefaultPerformanceMonitor.Names.Increment));
			this.Counters.AddRange(DefaultPerformanceMonitor.OpMonitor.CreateCounters(DefaultPerformanceMonitor.Names.Decrement));
			this.Counters.AddRange(DefaultPerformanceMonitor.OpMonitor.CreateCounters(DefaultPerformanceMonitor.Names.Append));
			this.Counters.AddRange(DefaultPerformanceMonitor.OpMonitor.CreateCounters(DefaultPerformanceMonitor.Names.Prepend));
		}
	}
}
