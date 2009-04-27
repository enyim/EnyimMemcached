using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.Globalization;
using System.Net;
using System.Collections.ObjectModel;

namespace Enyim.Caching.Configuration
{
	/// <summary>
	/// Represents a collection of <see cref="T:EndPointElement"/> instances. This class cannot be inherited.
	/// </summary>
	public sealed class EndPointElementCollection : ConfigurationElementCollection
	{
		/// <summary>
		/// Creates a new <see cref="T:ConfigurationElement"/>.
		/// </summary>
		/// <returns>A new <see cref="T:ConfigurationElement"/>.</returns>
		protected override ConfigurationElement CreateNewElement()
		{
			return new EndPointElement();
		}

		/// <summary>
		/// Gets the element key for a specified configuration element when overridden in a derived class.
		/// </summary>
		/// <param name="element">The <see cref="T:ConfigurationElement"/> to return the key for. </param>
		/// <returns>An <see cref="T:Object"/> that acts as the key for the specified <see cref="T:ConfigurationElement"/>.</returns>
		protected override object GetElementKey(ConfigurationElement element)
		{
			EndPointElement ep = (EndPointElement)element;

			return String.Concat(ep.Address, ":", ep.Port.ToString(CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Helper method; converts the collection into an <see cref="T:IPEndPoint"/> collection for the interface implementation.
		/// </summary>
		/// <returns></returns>
		internal IList<IPEndPoint> ToIPEndPointCollection()
		{
			List<IPEndPoint> retval = new List<IPEndPoint>(this.Count);
			foreach (EndPointElement e in this)
			{
				retval.Add(e.EndPoint);
			}

			return retval.AsReadOnly();
		}
	}
}

#region [ License information          ]
/* ************************************************************
 *
 * Copyright (c) Attila Kiskó, enyim.com
 *
 * This source code is subject to terms and conditions of 
 * Microsoft Permissive License (Ms-PL).
 * 
 * A copy of the license can be found in the License.html
 * file at the root of this distribution. If you can not 
 * locate the License, please send an email to a@enyim.com
 * 
 * By using this source code in any fashion, you are 
 * agreeing to be bound by the terms of the Microsoft 
 * Permissive License.
 *
 * You must not remove this notice, or any other, from this
 * software.
 *
 * ************************************************************/
#endregion