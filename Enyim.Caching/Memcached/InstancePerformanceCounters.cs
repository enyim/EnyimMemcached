using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Enyim.Caching.Memcached
{
	internal class InstancePerformanceCounters : IPerformanceCounters
	{
		private static InstancePerformanceCounters GlobalInstance = new InstancePerformanceCounters(String.Empty);

		private bool global;
		private string instanceName;

		private PerformanceCounter pcGet_Total;
		private PerformanceCounter pcGet_PerSec;
		private PerformanceCounter pcGet_Hits;
		private PerformanceCounter pcGet_Misses;
		private PerformanceCounter pcGet_HitRatio;
		private PerformanceCounter pcGet_HitRatioBase;

		private PerformanceCounter pcStore_Total;
		private PerformanceCounter pcStore_PerSec;
		private PerformanceCounter pcStore_Success;

		private PerformanceCounter pcAdd_Total;
		private PerformanceCounter pcAdd_PerSec;
		private PerformanceCounter pcAdd_Success;

		private PerformanceCounter pcReplace_Total;
		private PerformanceCounter pcReplace_PerSec;
		private PerformanceCounter pcReplace_Success;

		private PerformanceCounter pcAppend_Total;
		private PerformanceCounter pcAppend_PerSec;
		private PerformanceCounter pcAppend_Success;

		private PerformanceCounter pcPrepend_Total;
		private PerformanceCounter pcPrepend_PerSec;
		private PerformanceCounter pcPrepend_Success;

		private PerformanceCounter pcSet_Total;
		private PerformanceCounter pcSet_PerSec;
		private PerformanceCounter pcSet_Success;

		private PerformanceCounter pcCAS_Total;
		private PerformanceCounter pcCAS_PerSec;
		private PerformanceCounter pcCAS_Success;

		private PerformanceCounter pcDelete_Total;
		private PerformanceCounter pcDelete_PerSec;
		private PerformanceCounter pcDelete_Success;

		public InstancePerformanceCounters(MemcachedNode node) : this(node.EndPoint.ToString()) { }

		public InstancePerformanceCounters(string name)
		{
			this.global = String.IsNullOrEmpty(name);
			this.instanceName = this.global ? "_Total" : name;

			this.pcGet_Total = this.CreateCounter(Names.Get_Total);
			this.pcGet_PerSec = this.CreateCounter(Names.Get_PerSec);
			this.pcGet_Hits = this.CreateCounter(Names.Get_Hits);
			this.pcGet_Misses = this.CreateCounter(Names.Get_Misses);
			this.pcGet_HitRatio = this.CreateCounter(Names.Get_HitRatio);
			this.pcGet_HitRatioBase = this.CreateCounter(Names.Get_HitRatioBase);

			this.pcStore_Total = this.CreateCounter(Names.Store_Total);
			this.pcStore_PerSec = this.CreateCounter(Names.Store_PerSec);
			this.pcStore_Success = this.CreateCounter(Names.Store_Success);

			this.pcAdd_Total = this.CreateCounter(Names.Add_Total);
			this.pcAdd_PerSec = this.CreateCounter(Names.Add_PerSec);
			this.pcAdd_Success = this.CreateCounter(Names.Add_Success);

			this.pcReplace_Total = this.CreateCounter(Names.Replace_Total);
			this.pcReplace_PerSec = this.CreateCounter(Names.Replace_PerSec);
			this.pcReplace_Success = this.CreateCounter(Names.Replace_Success);

			this.pcAppend_Total = this.CreateCounter(Names.Append_Total);
			this.pcAppend_PerSec = this.CreateCounter(Names.Append_PerSec);
			this.pcAppend_Success = this.CreateCounter(Names.Append_Success);

			this.pcPrepend_Total = this.CreateCounter(Names.Prepend_Total);
			this.pcPrepend_PerSec = this.CreateCounter(Names.Prepend_PerSec);
			this.pcPrepend_Success = this.CreateCounter(Names.Prepend_Success);

			this.pcSet_Total = this.CreateCounter(Names.Set_Total);
			this.pcSet_PerSec = this.CreateCounter(Names.Set_PerSec);
			this.pcSet_Success = this.CreateCounter(Names.Set_Success);

			this.pcCAS_Total = this.CreateCounter(Names.CAS_Total);
			this.pcCAS_PerSec = this.CreateCounter(Names.CAS_PerSec);
			this.pcCAS_Success = this.CreateCounter(Names.CAS_Success);

			this.pcDelete_Total = this.CreateCounter(Names.Delete_Total);
			this.pcDelete_PerSec = this.CreateCounter(Names.Delete_PerSec);
			this.pcDelete_Success = this.CreateCounter(Names.Delete_Success);
		}

		private PerformanceCounter CreateCounter(string name)
		{
			PerformanceCounter retval = new PerformanceCounter(Names.CategoryName, name, this.instanceName, false);
			retval.RawValue = 0;

			return retval;
		}

		public void LogGet(bool success)
		{
			pcGet_PerSec.Increment();
			pcGet_Total.Increment();
			pcGet_HitRatioBase.Increment();

			if (success)
			{
				pcGet_Hits.Increment();
				pcGet_HitRatio.Increment();
			}
			else
			{
				pcGet_Misses.Increment();
			}

			if (!this.global)
				GlobalInstance.LogGet(success);
		}

		public void LogStore(StoreCommand cmd, bool success)
		{
			pcStore_PerSec.Increment();
			pcStore_Total.Increment();

			switch (cmd)
			{
				case StoreCommand.Add:
					pcAdd_PerSec.Increment();
					pcAdd_Total.Increment();
					break;

				case StoreCommand.Append:
					pcAppend_PerSec.Increment();
					pcAppend_Total.Increment();
					break;

				case StoreCommand.CheckAndSet:
					pcCAS_PerSec.Increment();
					pcCAS_Total.Increment();
					break;

				case StoreCommand.Prepend:
					pcPrepend_PerSec.Increment();
					pcPrepend_Total.Increment();
					break;

				case StoreCommand.Replace:
					pcReplace_PerSec.Increment();
					pcReplace_Total.Increment();
					break;

				case StoreCommand.Set:
					pcSet_PerSec.Increment();
					pcSet_Total.Increment();
					break;
			}

			if (success)
			{
				pcStore_Success.Increment();

				switch (cmd)
				{
					case StoreCommand.Add:
						pcAdd_Success.Increment();
						break;

					case StoreCommand.Append:
						pcAppend_Success.Increment();
						break;

					case StoreCommand.CheckAndSet:
						pcCAS_Success.Increment();
						break;

					case StoreCommand.Prepend:
						pcPrepend_Success.Increment();
						break;

					case StoreCommand.Replace:
						pcReplace_Success.Increment();
						break;

					case StoreCommand.Set:
						pcSet_Success.Increment();
						break;
				}
			}

			if (!this.global)
				GlobalInstance.LogStore(cmd, success);
		}

		public void LogDelete(bool success)
		{
			pcDelete_PerSec.Increment();
			pcDelete_Total.Increment();

			if (success)
				pcDelete_Success.Increment();

			if (!this.global)
				GlobalInstance.LogDelete(success);
		}

		#region [ public static class Names    ]
		public static class Names
		{
			public const string Get_Total = "Gets Total";
			public const string Get_PerSec = "Gets/Sec";
			public const string Get_Hits = "Get Hits";
			public const string Get_Misses = "Get Misses";
			public const string Get_HitRatio = "Get Hit Ratio";
			public const string Get_HitRatioBase = "Get Ratio Base";

			public const string Store_Total = "Stores Total";
			public const string Store_PerSec = "Stores/Sec";
			public const string Store_Success = "Stores Successful";

			public const string Add_Total = "Adds Total";
			public const string Add_PerSec = "Adds/Sec";
			public const string Add_Success = "Adds Successful";

			public const string Replace_Total = "Replaces Total";
			public const string Replace_PerSec = "Replaces/Sec";
			public const string Replace_Success = "Replaces Successful";

			public const string Append_Total = "Appends Total";
			public const string Append_PerSec = "Appends/Sec";
			public const string Append_Success = "Appends Successful";

			public const string Prepend_Total = "Prepends Total";
			public const string Prepend_PerSec = "Prepends/Sec";
			public const string Prepend_Success = "Prepends Successful";

			public const string Set_Total = "Sets Total";
			public const string Set_PerSec = "Sets/Sec";
			public const string Set_Success = "Sets Successful";

			public const string CAS_Total = "CAS Total";
			public const string CAS_PerSec = "CAS/Sec";
			public const string CAS_Success = "CAS Successful";

			public const string Delete_Total = "Deletes Total";
			public const string Delete_PerSec = "Deletes/Sec";
			public const string Delete_Success = "Deletes Successful";

			public const string CategoryName = "Enyim.Caching.Memcached";
		}
		#endregion
		#region [ IPerformanceCounters         ]

		void IPerformanceCounters.LogGet(bool success)
		{
			this.LogGet(success);
		}

		void IPerformanceCounters.LogStore(StoreCommand cmd, bool success)
		{
			this.LogStore(cmd, success);
		}

		void IPerformanceCounters.LogDelete(bool success)
		{
			this.LogDelete(success);
		}

		#endregion
		#region [ IDisposable                  ]
		public void Dispose()
		{
			if (this.pcAdd_PerSec != null)
			{
				#region [ Dispose the counters         ]
				this.pcGet_Total.Dispose();
				this.pcGet_PerSec.Dispose();
				this.pcGet_Hits.Dispose();
				this.pcGet_Misses.Dispose();
				this.pcGet_HitRatio.Dispose();
				this.pcGet_HitRatioBase.Dispose();

				this.pcStore_Total.Dispose();
				this.pcStore_PerSec.Dispose();
				this.pcStore_Success.Dispose();

				this.pcAdd_Total.Dispose();
				this.pcAdd_PerSec.Dispose();
				this.pcAdd_Success.Dispose();

				this.pcReplace_Total.Dispose();
				this.pcReplace_PerSec.Dispose();
				this.pcReplace_Success.Dispose();

				this.pcAppend_Total.Dispose();
				this.pcAppend_PerSec.Dispose();
				this.pcAppend_Success.Dispose();

				this.pcPrepend_Total.Dispose();
				this.pcPrepend_PerSec.Dispose();
				this.pcPrepend_Success.Dispose();

				this.pcSet_Total.Dispose();
				this.pcSet_PerSec.Dispose();
				this.pcSet_Success.Dispose();

				this.pcCAS_Total.Dispose();
				this.pcCAS_PerSec.Dispose();
				this.pcCAS_Success.Dispose();

				this.pcDelete_Total.Dispose();
				this.pcDelete_PerSec.Dispose();
				this.pcDelete_Success.Dispose();
				#endregion
				#region [ Release the references       ]
				this.pcGet_Total = null;
				this.pcGet_PerSec = null;
				this.pcGet_Hits = null;
				this.pcGet_Misses = null;
				this.pcGet_HitRatio = null;
				this.pcGet_HitRatioBase = null;

				this.pcStore_Total = null;
				this.pcStore_PerSec = null;
				this.pcStore_Success = null;

				this.pcAdd_Total = null;
				this.pcAdd_PerSec = null;
				this.pcAdd_Success = null;

				this.pcReplace_Total = null;
				this.pcReplace_PerSec = null;
				this.pcReplace_Success = null;

				this.pcAppend_Total = null;
				this.pcAppend_PerSec = null;
				this.pcAppend_Success = null;

				this.pcPrepend_Total = null;
				this.pcPrepend_PerSec = null;
				this.pcPrepend_Success = null;

				this.pcSet_Total = null;
				this.pcSet_PerSec = null;
				this.pcSet_Success = null;

				this.pcCAS_Total = null;
				this.pcCAS_PerSec = null;
				this.pcCAS_Success = null;

				this.pcDelete_Total = null;
				this.pcDelete_PerSec = null;
				this.pcDelete_Success = null;
				#endregion
			}
		}

		void IDisposable.Dispose()
		{
			this.Dispose();
		}
		#endregion
	}

	class NullPerformanceCounter : IPerformanceCounters
	{
		#region [ IPerformanceCounters         ]

		void IPerformanceCounters.LogGet(bool success) { }
		void IPerformanceCounters.LogStore(StoreCommand cmd, bool success) { }
		void IPerformanceCounters.LogDelete(bool success) { }

		#endregion
		#region [ IDisposable                  ]

		void IDisposable.Dispose()
		{
		}

		#endregion
	}

	interface IPerformanceCounters : IDisposable
	{
		void LogGet(bool success);
		void LogStore(StoreCommand cmd, bool success);
		void LogDelete(bool success);
	}
}