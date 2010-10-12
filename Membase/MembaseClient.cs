using System;
using System.Linq;
using System.Configuration;
using Enyim.Caching;
using Enyim.Caching.Memcached;
using Membase.Configuration;

namespace Membase
{
	/// <summary>
	/// Client which can be used to connect to NothScale's Memcached and Membase servers.
	/// </summary>
	public class MembaseClient : MemcachedClient
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(MembaseClient));
		private static IMembaseClientConfiguration DefaultConfig = (IMembaseClientConfiguration)ConfigurationManager.GetSection("membase");

		private MembasePool nsPool;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Membase.MembasePool" /> class using the default configuration and bucket.
		/// </summary>
		/// <remarks>The configuration is taken from the /configuration/membase section.</remarks>
		public MembaseClient() :
			this(DefaultConfig, null) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Membase.MembasePool" /> class 
		/// using the default configuration and the specified bucket name.
		/// </summary>
		/// <param name="bucketName">The name of the bucket this client will connect to.</param>
		public MembaseClient(string bucketName) :
			this(DefaultConfig, bucketName) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Membase.MembasePool" /> class 
		/// using the specified configuration and bucket name.
		/// </summary>
		/// <param name="sectionName">The name of the configuration section to load.</param>
		/// <param name="bucketName">The name of the bucket this client will connect to.</param>
		public MembaseClient(string sectionName, string bucketName) :
			this((IMembaseClientConfiguration)ConfigurationManager.GetSection(sectionName), bucketName) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Membase.MembasePool" /> class 
		/// using a custom configuration provider.
		/// </summary>
		/// <param name="configuration">The custom configuration provider.</param>
		public MembaseClient(IMembaseClientConfiguration configuration) :
			this(configuration, null)
		{
			this.nsPool = (MembasePool)this.Pool;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Membase.MembasePool" /> class 
		/// using a custom configuration provider and the specified bucket name.
		/// </summary>
		/// <param name="configuration">The custom configuration provider.</param>
		/// <param name="bucketName">The name of the bucket this client will connect to. Note: this will override the configuration's BucketName property.</param>
		public MembaseClient(IMembaseClientConfiguration configuration, string bucketName) :
			base(new MembasePool(configuration, IsDefaultBucket(bucketName) ? null : bucketName),
					configuration.CreateKeyTransformer(),
					configuration.CreateTranscoder()) { }

		private static bool IsDefaultBucket(string name)
		{
			return String.IsNullOrEmpty(name) || name == "default";
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

					return true;
				}
			}

			value = null;
			cas = 0;

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

				cas = command.CasValue;

				if (success)
					return command.Result;
			}

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

				return retval;
			}

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

					return false;
				}

				var command = this.Pool.OperationFactory.Store(mode, hashedKey, item, expires, cas);
				var retval = ExecuteWithRedirect(node, command);

				cas = command.CasValue;

				return retval;
			}

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
