using System;
using System.Collections.Generic;
using System.Configuration;

namespace NorthScale.Store.Configuration
{
	/// <summary>
	/// Represents a collection of <see cref="T:EndPointElement"/> instances. This class cannot be inherited.
	/// </summary>
	public sealed class UriElementCollection : ConfigurationElementCollection
	{
		/// <summary>
		/// Creates a new <see cref="T:ConfigurationElement"/>.
		/// </summary>
		/// <returns>A new <see cref="T:ConfigurationElement"/>.</returns>
		protected override ConfigurationElement CreateNewElement()
		{
			return new UriElement();
		}

		/// <summary>
		/// Gets the element key for a specified configuration element when overridden in a derived class.
		/// </summary>
		/// <param name="element">The <see cref="T:ConfigurationElement"/> to return the key for. </param>
		/// <returns>An <see cref="T:Object"/> that acts as the key for the specified <see cref="T:ConfigurationElement"/>.</returns>
		protected override object GetElementKey(ConfigurationElement element)
		{
			UriElement e = (UriElement)element;

			return e.Uri;
		}

		/// <summary>
		/// Helper method; converts the collection into an <see cref="T:IPEndPoint"/> collection for the interface implementation.
		/// </summary>
		/// <returns></returns>
		public IList<Uri> ToUriCollection()
		{
			List<Uri> retval = new List<Uri>(this.Count);
			foreach (UriElement e in this)
			{
				retval.Add(e.Uri);
			}

			return retval.AsReadOnly();
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
