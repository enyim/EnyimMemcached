using System;
using System.Configuration;
using Enyim.Caching;
using Enyim.Caching.Memcached;
using NorthScale.Store.Configuration;

namespace NorthScale.Store
{
	/// <summary>
	/// Client which can be used to connect to NothScale's Memcached and Membase servers.
	/// </summary>
	public class NorthScaleClient : MemcachedClient
	{
		private static INorthScaleClientConfiguration DefaultConfig = (INorthScaleClientConfiguration)ConfigurationManager.GetSection("northscale");

		/// <summary>
		/// Initializes a new instance of the <see cref="T:NorthScale.Store.NorthScalePool" /> class using the default configuration and bucket.
		/// </summary>
		/// <remarks>The configuration is taken from the /configuration/northscale section.</remarks>
		public NorthScaleClient() :
			this(DefaultConfig, null) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:NorthScale.Store.NorthScalePool" /> class 
		/// using the default configuration and the specified bucket name.
		/// </summary>
		/// <param name="bucketName">The name of the bucket this client will connect to.</param>
		public NorthScaleClient(string bucketName) :
			this(DefaultConfig, bucketName) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:NorthScale.Store.NorthScalePool" /> class 
		/// using the specified configuration and bucket name.
		/// </summary>
		/// <param name="sectionName">The name of the configuration section to load.</param>
		/// <param name="bucketName">The name of the bucket this client will connect to.</param>
		public NorthScaleClient(string sectionName, string bucketName) :
			this((INorthScaleClientConfiguration)ConfigurationManager.GetSection(sectionName), bucketName) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:NorthScale.Store.NorthScalePool" /> class 
		/// using a custom configuration provider.
		/// </summary>
		/// <param name="configuration">The custom configuration provider.</param>
		public NorthScaleClient(INorthScaleClientConfiguration configuration) :
			this(configuration, null) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:NorthScale.Store.NorthScalePool" /> class 
		/// using a custom configuration provider and the specified bucket name.
		/// </summary>
		/// <param name="configuration">The custom configuration provider.</param>
		/// <param name="bucketName">The name of the bucket this client will connect to. Note: this will override the configuration's BucketName property.</param>
		public NorthScaleClient(INorthScaleClientConfiguration configuration, string bucketName) :
			base(new NorthScalePool(configuration, IsDefaultBucket(bucketName) ? null : bucketName),
					configuration.CreateKeyTransformer(),
					configuration.CreateTranscoder()) { }

		private static bool IsDefaultBucket(string name)
		{
			return String.IsNullOrEmpty(name) || name == "default";
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
