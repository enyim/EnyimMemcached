using System;
using System.Configuration;
using Enyim.Caching;
using Enyim.Caching.Memcached;
using NorthScale.Store.Configuration;

namespace NorthScale.Store
{
	public class NorthScaleClient : MemcachedClient
	{
		private static INorthScaleClientConfiguration DefaultConfig = (INorthScaleClientConfiguration)ConfigurationManager.GetSection("northscale");

		public NorthScaleClient() :
			this(DefaultConfig, null) { }

		public NorthScaleClient(string bucketName) :
			this(DefaultConfig, bucketName) { }

		public NorthScaleClient(string sectionName, string bucketName) :
			this((INorthScaleClientConfiguration)ConfigurationManager.GetSection("sectionName"), bucketName) { }

		public NorthScaleClient(INorthScaleClientConfiguration configuration) :
			this(configuration, null) { }

		public NorthScaleClient(INorthScaleClientConfiguration configuration, string bucketName) :
			base(new NorthScalePool(configuration, IsDefaultBucket(bucketName) ? null : bucketName),
					CreateAuthProvider(configuration, bucketName),
					MemcachedProtocol.Binary) { }

		private static bool IsDefaultBucket(string name)
		{
			return String.IsNullOrEmpty(name) || name == "default";
		}

		private static ISaslAuthenticationProvider CreateAuthProvider(INorthScaleClientConfiguration configuration, string bucketName)
		{
			if (IsDefaultBucket(bucketName) && IsDefaultBucket(bucketName = configuration.Bucket))
				return null;

			// moxi (when using the proxy port) only accepts an empty authzid
			return new PlainTextAuthenticator(null, bucketName, bucketName);
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
