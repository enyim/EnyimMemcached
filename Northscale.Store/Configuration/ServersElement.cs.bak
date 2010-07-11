using System.Configuration;

namespace NorthScale.Store.Configuration
{
	/// <summary>
	/// Configures the <see cref="T:MemcachedClient"/>. This class cannot be inherited.
	/// </summary>
	public sealed class ServersElement : ConfigurationElement
	{
		/// <summary>
		/// Gets or sets the name of the bucket to be used. Can be overriden at the pool's constructor, and if not pecified the "default" bucket will be used.
		/// </summary>
		[ConfigurationProperty("bucket", IsRequired = false)]
		public string Bucket
		{
			get { return (string)base["bucket"]; }
			set { base["bucket"] = value; }
		}

		/// <summary>
		/// Gets or sets the user name used to connect to a secured cluster
		/// </summary>
		[ConfigurationProperty("userName", IsRequired = false)]
		public string UserName
		{
			get { return (string)base["userName"]; }
			set { base["userName"] = value; }
		}

		/// <summary>
		/// Gets or sets the password used to connect to a secured cluster
		/// </summary>
		[ConfigurationProperty("password", IsRequired = false)]
		public string Password
		{
			get { return (string)base["password"]; }
			set { base["password"] = value; }
		}

		/// <summary>
		/// Returns a collection of nodes in the cluster the client should use to retrieve the Memcached nodes.
		/// </summary>
		[ConfigurationProperty("", IsRequired = true, IsDefaultCollection = true)]
		public UriElementCollection Urls
		{
			get { return (UriElementCollection)base[""]; }
		}

		/// <summary>
		/// Determines which port the client should use to connect to the nodes
		/// </summary>
		[ConfigurationProperty("port", IsRequired = false, DefaultValue=BucketPortType.Direct)]
		public BucketPortType Port
		{
			get { return (BucketPortType)base["port"]; }
			set { base["port"] = value; }
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
