using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;

namespace Enyim.Caching.Memcached
{
	[RunInstaller(true)]
	public class InstancePerformanceCountersInstaller : PerformanceCounterInstaller
	{
		public InstancePerformanceCountersInstaller()
		{
			this.CategoryName = InstancePerformanceCounters.Names.CategoryName;

			this.CategoryType = PerformanceCounterCategoryType.MultiInstance;
			this.UninstallAction = UninstallAction.Remove;

			CreateGroup(this.Counters, "Append", InstancePerformanceCounters.Names.Append_PerSec, InstancePerformanceCounters.Names.Append_Success, InstancePerformanceCounters.Names.Append_Total);
			CreateGroup(this.Counters, "Prepend", InstancePerformanceCounters.Names.Prepend_PerSec, InstancePerformanceCounters.Names.Prepend_Success, InstancePerformanceCounters.Names.Prepend_Total);

			CreateGroup(this.Counters, "Delete", InstancePerformanceCounters.Names.Delete_PerSec, InstancePerformanceCounters.Names.Delete_Success, InstancePerformanceCounters.Names.Delete_Total);
			
			CreateGroup(this.Counters, "Add", InstancePerformanceCounters.Names.Add_PerSec, InstancePerformanceCounters.Names.Add_Success, InstancePerformanceCounters.Names.Add_Total);
			CreateGroup(this.Counters, "CAS", InstancePerformanceCounters.Names.CAS_PerSec, InstancePerformanceCounters.Names.CAS_Success, InstancePerformanceCounters.Names.CAS_Total);
			CreateGroup(this.Counters, "Replace", InstancePerformanceCounters.Names.Replace_PerSec, InstancePerformanceCounters.Names.Replace_Success, InstancePerformanceCounters.Names.Replace_Total);
			CreateGroup(this.Counters, "Set", InstancePerformanceCounters.Names.Set_PerSec, InstancePerformanceCounters.Names.Set_Success, InstancePerformanceCounters.Names.Set_Total);

			CreateGroup(this.Counters, "Get", InstancePerformanceCounters.Names.Get_PerSec, InstancePerformanceCounters.Names.Get_Hits, InstancePerformanceCounters.Names.Get_Total);
			this.Counters.Add(new CounterCreationData(InstancePerformanceCounters.Names.Get_Misses,
				"The number of [Get] operations which were not served from the cache.",
				PerformanceCounterType.NumberOfItems64));

			this.Counters.Add(new CounterCreationData(InstancePerformanceCounters.Names.Get_HitRatio,
				"Ratio of successful [Get] operations based on the total number of [Get] operations.",
				PerformanceCounterType.RawFraction));
			this.Counters.Add(new CounterCreationData(InstancePerformanceCounters.Names.Get_HitRatioBase, String.Empty, PerformanceCounterType.RawBase));
		}

		static void CreateGroup(CounterCreationDataCollection counters, string operation, string persecName, string successName, string totalName)
		{
			counters.Add(new CounterCreationData(
						persecName,
						String.Format("The total number of [{0}] operations per sec.", operation),
						PerformanceCounterType.RateOfCountsPerSecond64));

			counters.Add(new CounterCreationData(
						totalName,
						String.Format("The total number of [{0}] operations.", operation),
						PerformanceCounterType.NumberOfItems64));

			counters.Add(new CounterCreationData(
						successName,
						String.Format("The total number of successful [{0}] operations.", operation),
						PerformanceCounterType.NumberOfItems64));
		}
	}
}
