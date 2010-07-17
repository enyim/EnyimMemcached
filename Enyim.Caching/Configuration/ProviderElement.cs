using System;
using System.ComponentModel;
using System.Configuration;
using System.Collections.Generic;
using Enyim.Caching.Memcached;
using Enyim.Reflection;

namespace Enyim.Caching.Configuration
{
	/// <summary>
	/// This element defines the IMemcachedNodeLocator used by the client.
	/// </summary>
	/// <remarks>This deprecates the nodeLocator attribute (which will be removed later).</remarks>
	public sealed class ProviderElement<T> : ConfigurationElement
		where T : class
	{
		// TODO make this element play nice with the configuration system (allow saving, etc.)
		private Dictionary<string, string> parameters = new Dictionary<string, string>();
		private IProviderFactory<T> factoryInstance;

		/// <summary>
		/// Gets or sets the type of the provider.
		/// </summary>
		[ConfigurationProperty("type", IsRequired = false), TypeConverter(typeof(TypeNameConverter))]
		public Type Type
		{
			get { return (Type)base["type"]; }
			set
			{
				ConfigurationHelper.CheckForInterface(value, typeof(T));
				base["type"] = value;
			}
		}

		/// <summary>
		/// Gets or sets the type of the provider factory.
		/// </summary>
		[ConfigurationProperty("factory", IsRequired = false), TypeConverter(typeof(TypeNameConverter))]
		public Type Factory
		{
			get { return (Type)base["factory"]; }
			set
			{
				ConfigurationHelper.CheckForInterface(value, typeof(IProviderFactory<T>));

				base["factory"] = value;
			}
		}

		protected override bool OnDeserializeUnrecognizedAttribute(string name, string value)
		{
			ConfigurationProperty property = new ConfigurationProperty(name, typeof(string), value);
			base[property] = value;

			this.parameters[name] = value;

			return true;
		}

		/// <summary>
		/// Creates the provider by using the factory (if present) or directly instantiating by type name
		/// </summary>
		/// <returns></returns>
		public T CreateInstance()
		{
			//check if we have a factory
			if (this.factoryInstance == null)
			{
				var type = this.Factory;
				if (type != null)
				{
					this.factoryInstance = (IProviderFactory<T>)Activator.CreateInstance(type);
					this.factoryInstance.Initialize(this.parameters);
				}
			}

			// no factory, use the provider type
			if (this.factoryInstance == null)
			{
				var type = this.Type;

				if (type == null)
					return null;

				return (T)FastActivator2.Create(type);
			}

			return factoryInstance.Create();
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
