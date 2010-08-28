using System;
using System.ComponentModel;
using System.Configuration;

namespace NorthScale.Store.Configuration
{
	/// <summary>
	/// Represents a configuration element that contains a Memcached node address. This class cannot be inherited. 
	/// </summary>
	public sealed class UriElement : ConfigurationElement
	{
		/// <summary>
		/// Gets or sets the ip address of the node.
		/// </summary>
		[ConfigurationProperty("uri", IsRequired = true, IsKey = true), UriValidator, TypeConverter(typeof(UriConverter))]
		public Uri Uri
		{
			get { return (Uri)base["uri"]; }
			set { base["uri"] = value; }
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
