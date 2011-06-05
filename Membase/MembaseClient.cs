using System;
using System.Linq;
using System.Configuration;
using Enyim.Caching;
using Enyim.Caching.Memcached;
using Membase.Configuration;
using System.Collections.Generic;
using System.Threading;
using KVP_SU = System.Collections.Generic.KeyValuePair<string, ulong>;

namespace Membase
{
	/// <summary>
	/// Client which can be used to connect to NothScale's Memcached and Membase servers.
	/// </summary>
	public class MembaseClient : MemcachedClient
	{
		private static readonly Enyim.Caching.ILog log = Enyim.Caching.LogManager.GetLogger(typeof(MembaseClient));
		private static readonly IMembaseClientConfiguration DefaultConfig = (IMembaseClientConfiguration)ConfigurationManager.GetSection("membase");

		private IMembaseServerPool poolInstance;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Membase.MembaseClient" /> class using the default configuration and bucket.
		/// </summary>
		/// <remarks>The configuration is taken from the /configuration/membase section.</remarks>
		public MembaseClient() : this(DefaultConfig) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Membase.MembaseClient" /> class using the default configuration and the specified bucket.
		/// </summary>
		/// <remarks>The configuration is taken from the /configuration/membase section.</remarks>
		public MembaseClient(string bucketName, string bucketPassword) :
			this(DefaultConfig, bucketName, bucketPassword) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Membase.MembaseClient" /> class using a custom configuration provider.
		/// </summary>
		/// <param name="configuration">The custom configuration provider.</param>
		public MembaseClient(IMembaseClientConfiguration configuration) :
			this(configuration, configuration.Bucket, configuration.BucketPassword) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Membase.MembaseClient" /> class using the specified configuration 
		/// section and the specified bucket.
		/// </summary>
		/// <param name="sectionName">The name of the configuration section to load.</param>
		/// <param name="bucketName">The name of the bucket this client will connect to.</param>
		/// <param name="bucketPassword">The password of the bucket this client will connect to.</param>
		public MembaseClient(string sectionName, string bucketName, string bucketPassword) :
			this((IMembaseClientConfiguration)ConfigurationManager.GetSection(sectionName), bucketName, bucketPassword) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Membase.MembaseClient" /> class 
		/// using a custom configuration provider and the specified bucket name and password.
		/// </summary>
		/// <param name="configuration">The custom configuration provider.</param>
		/// <param name="bucketName">The name of the bucket this client will connect to.</param>
		/// <param name="bucketPassword">The password of the bucket this client will connect to.</param>
		public MembaseClient(IMembaseClientConfiguration configuration, string bucketName, string bucketPassword) :
			base(new MembasePool(configuration, bucketName, bucketPassword),
					configuration.CreateKeyTransformer(),
					configuration.CreateTranscoder(),
					configuration.CreatePerformanceMonitor())
		{
			this.poolInstance = (IMembaseServerPool)this.Pool;
		}

		/// <summary>Obsolete. Use .ctor(bucket, password) to explicitly set the bucket password.</summary>
		[Obsolete("Use .ctor(bucket, password) to explicitly set the bucket password.", true)]
		public MembaseClient(string bucketName)
		{
			throw new InvalidOperationException("Use .ctor(bucket, password) to explicitly set the bucket password.");
		}

		/// <summary>Obsolete. Use .ctor(config, bucket, password) to explicitly set the bucket password.</summary>
		[Obsolete("Use .ctor(config, bucket, password) to explicitly set the bucket password.", true)]
		public MembaseClient(IMembaseClientConfiguration configuration, string bucketName)
		{
			throw new InvalidOperationException("Use .ctor(config, bucket, password) to explicitly set the bucket password.");
		}

		protected override bool PerformTryGet(string key, out ulong cas, out object value)
		{
			var hashedKey = this.KeyTransformer.Transform(key);
			var node = this.Pool.Locate(hashedKey);

			if (node != null)
			{
				var command = this.Pool.OperationFactory.Get(hashedKey);

				if (ExecuteWithRedirect(node, command))
				{
					value = this.Transcoder.Deserialize(command.Result);
					cas = command.CasValue;
					if (this.PerformanceMonitor != null) this.PerformanceMonitor.Get(1, true);

					return true;
				}
			}

			value = null;
			cas = 0;
			if (this.PerformanceMonitor != null) this.PerformanceMonitor.Get(1, false);

			return false;
		}

		protected override ulong PerformMutate(MutationMode mode, string key, ulong defaultValue, ulong delta, uint expires, ref ulong cas)
		{
			var hashedKey = this.KeyTransformer.Transform(key);
			var node = this.Pool.Locate(hashedKey);

			if (node != null)
			{
				var command = this.Pool.OperationFactory.Mutate(mode, hashedKey, defaultValue, delta, expires, cas);
				var success = ExecuteWithRedirect(node, command);

				if (this.PerformanceMonitor != null) this.PerformanceMonitor.Mutate(mode, 1, success);

				cas = command.CasValue;

				if (success)
					return command.Result;
			}

			if (this.PerformanceMonitor != null) this.PerformanceMonitor.Mutate(mode, 1, false);

			return defaultValue;
		}

		protected override bool PerformConcatenate(ConcatenationMode mode, string key, ref ulong cas, ArraySegment<byte> data)
		{
			var hashedKey = this.KeyTransformer.Transform(key);
			var node = this.Pool.Locate(hashedKey);

			if (node != null)
			{
				var command = this.Pool.OperationFactory.Concat(mode, hashedKey, 0, data);
				var retval = this.ExecuteWithRedirect(node, command);

				cas = command.CasValue;
				if (this.PerformanceMonitor != null) this.PerformanceMonitor.Concatenate(mode, 1, true);

				return retval;
			}

			if (this.PerformanceMonitor != null) this.PerformanceMonitor.Concatenate(mode, 1, false);

			return false;
		}

		protected override bool PerformStore(StoreMode mode, string key, object value, uint expires, ref ulong cas)
		{
			var hashedKey = this.KeyTransformer.Transform(key);
			var node = this.Pool.Locate(hashedKey);

			if (node != null)
			{
				CacheItem item;

				try { item = this.Transcoder.Serialize(value); }
				catch (Exception e)
				{
					log.Error(e);

					if (this.PerformanceMonitor != null) this.PerformanceMonitor.Store(mode, 1, false);

					return false;
				}

				var command = this.Pool.OperationFactory.Store(mode, hashedKey, item, expires, cas);
				var retval = ExecuteWithRedirect(node, command);

				cas = command.CasValue;

				if (this.PerformanceMonitor != null) this.PerformanceMonitor.Store(mode, 1, true);

				return retval;
			}

			if (this.PerformanceMonitor != null) this.PerformanceMonitor.Store(mode, 1, false);

			return false;
		}

		private bool ExecuteWithRedirect(IMemcachedNode startNode, ISingleItemOperation op)
		{
			if (startNode.Execute(op)) return true;

			var iows = op as IOperationWithState;

			// different op factory, we do not know how to retry
			if (iows == null)
				return false;

#if HAS_FORWARD_MAP
			// node responded with invalid vbucket
			// this should happen only when a node is in a transitioning state
			if (iows.State == OpState.InvalidVBucket)
			{
				// check if we have a forward-locator
				// (whihc supposedly reflects the state of the cluster when all vbuckets have been migrated succesfully)
				IMemcachedNodeLocator fl = this.nsPool.ForwardLocator;
				if (fl != null)
				{
					var nextNode = fl.Locate(op.Key);
					if (nextNode != null)
					{
						// the node accepted the requesta
						if (nextNode.Execute(op)) return true;
					}
				}
			}
#endif
			// still invalid vbucket, try all nodes in sequence
			if (iows.State == OperationState.InvalidVBucket)
			{
				var nodes = this.Pool.GetWorkingNodes();

				foreach (var node in nodes)
				{
					if (node.Execute(op))
						return true;

					// the node accepted our request so quit
					if (iows.State != OperationState.InvalidVBucket)
						break;
				}
			}

			return false;
		}

		public void Touch(string key, DateTime nextExpiration)
		{
			PerformTouch(key, GetExpiration(null, nextExpiration));
		}

		public void Touch(string key, TimeSpan nextExpiration)
		{
			PerformTouch(key, GetExpiration(nextExpiration, null));
		}

		protected void PerformTouch(string key, uint nextExpiration)
		{
			var hashedKey = this.KeyTransformer.Transform(key);
			var node = this.Pool.Locate(hashedKey);

			if (node != null)
			{
				var command = this.poolInstance.OperationFactory.Touch(key, nextExpiration);
				var retval = ExecuteWithRedirect(node, command);
			}
		}

		public object Get(string key, DateTime newExpiration)
		{
			object tmp;

			return this.TryGet(key, newExpiration, out tmp) ? tmp : null;
		}

		public T Get<T>(string key, DateTime newExpiration)
		{
			object tmp;

			return TryGet(key, newExpiration, out tmp) ? (T)tmp : default(T);
		}

		public bool TryGet(string key, DateTime newExpiration, out object value)
		{
			ulong cas = 0;

			return this.PerformTryGetAndTouch(key, MemcachedClient.GetExpiration(null, newExpiration), out cas, out value);
		}

		public CasResult<object> GetWithCas(string key, DateTime newExpiration)
		{
			return this.GetWithCas<object>(key, newExpiration);
		}

		public CasResult<T> GetWithCas<T>(string key, DateTime newExpiration)
		{
			CasResult<object> tmp;

			return this.TryGetWithCas(key, newExpiration, out tmp)
					? new CasResult<T> { Cas = tmp.Cas, Result = (T)tmp.Result }
					: new CasResult<T> { Cas = tmp.Cas, Result = default(T) };
		}

		public bool TryGetWithCas(string key, DateTime newExpiration, out CasResult<object> value)
		{
			object tmp;
			ulong cas;

			var retval = this.PerformTryGetAndTouch(key, MemcachedClient.GetExpiration(null, newExpiration), out cas, out tmp);

			value = new CasResult<object> { Cas = cas, Result = tmp };

			return retval;
		}

		protected bool PerformTryGetAndTouch(string key, uint nextExpiration, out ulong cas, out object value)
		{
			var hashedKey = this.KeyTransformer.Transform(key);
			var node = this.Pool.Locate(hashedKey);

			if (node != null)
			{
				var command = this.poolInstance.OperationFactory.GetAndTouch(hashedKey, nextExpiration);

				if (this.ExecuteWithRedirect(node, command))
				{
					value = this.Transcoder.Deserialize(command.Result);
					cas = command.CasValue;
					if (this.PerformanceMonitor != null) this.PerformanceMonitor.Get(1, true);

					return true;
				}
			}

			value = null;
			cas = 0;
			if (this.PerformanceMonitor != null) this.PerformanceMonitor.Get(1, false);

			return false;
		}

		public SyncResult Sync(string key, ulong cas, SyncMode mode)
		{
			return this.Sync(key, cas, mode, 0);
		}

		public SyncResult Sync(string key, ulong cas, SyncMode mode, int replicationCount)
		{
			var hashedKey = this.KeyTransformer.Transform(key);
			var node = this.Pool.Locate(hashedKey);

			var tmp = this.PerformMultiSync(mode, replicationCount, new[] { new KeyValuePair<string, ulong>(key, cas) });
			SyncResult retval;

			return tmp.TryGetValue(key, out retval)
				? retval
				: null;
		}

		public IDictionary<string, SyncResult> Sync(SyncMode mode, IEnumerable<KeyValuePair<string, ulong>> items)
		{
			return this.PerformMultiSync(mode, 0, items);
		}

		protected IDictionary<string, SyncResult> PerformMultiSync(SyncMode mode, int replicationCount, IEnumerable<KeyValuePair<string, ulong>> items)
		{
			// transform the keys and index them by hashed => original
			// the results will be mapped using this index
			var hashed = new Dictionary<string, string>();
			var hashedAndMapped = new Dictionary<IMemcachedNode, IList<KVP_SU>>();

			foreach (var k in items)
			{
				var hashedKey = this.KeyTransformer.Transform(k.Key);
				var node = this.Pool.Locate(hashedKey);

				if (node == null) continue;

				hashed[hashedKey] = k.Key;

				IList<KVP_SU> list;
				if (!hashedAndMapped.TryGetValue(node, out list))
					hashedAndMapped[node] = list = new List<KVP_SU>(4);

				list.Add(k);
			}

			var retval = new Dictionary<string, SyncResult>(hashed.Count);
			if (hashedAndMapped.Count == 0) return retval;

			using (var spin = new ReaderWriterLockSlim())
			using (var latch = new CountdownEvent(hashedAndMapped.Count))
			{
				//execute each list of keys on their respective node
				foreach (var slice in hashedAndMapped)
				{
					var node = slice.Key;
					var nodeKeys = slice.Value;

					var sync = this.poolInstance.OperationFactory.Sync(mode, slice.Value, replicationCount);

					#region result gathering
					// ExecuteAsync will not call the delegate if the
					// node was already in a failed state but will return false immediately
					var execSuccess = node.ExecuteAsync(sync, success =>
					{
						if (success)
							try
							{
								var result = sync.Result;

								if (result != null && result.Length > 0)
								{
									string original;

									foreach (var kvp in result)
										if (hashed.TryGetValue(kvp.Key, out original))
										{
											spin.EnterWriteLock();
											try
											{ retval[original] = kvp; }
											finally
											{ spin.ExitWriteLock(); }
										}
								}
							}
							catch (Exception e)
							{
								log.Error(e);
							}

						latch.Signal();
					});
					#endregion

					// signal the latch when the node fails immediately (e.g. it was already dead)
					if (!execSuccess) latch.Signal();
				}

				latch.Wait();
			}

			return retval;
		}

	}
}

#region [ License information          ]
/* ************************************************************
 * 
 *    Copyright (c) 2010 Attila Kiskó, enyim.com
 *    
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *    
 *        http://www.apache.org/licenses/LICENSE-2.0
 *    
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *    
 * ************************************************************/
#endregion
