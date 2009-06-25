using System;
using System.Collections.Generic;
using System.Text;

namespace Enyim.Caching.Memcached
{
	/// <summary>
	/// Defines a locator class whihc maps item keys to memcached servers.
	/// </summary>
	public interface IMemcachedNodeLocator
	{
		/// <summary>
		/// Initializes the locator.
		/// </summary>
		/// <param name="nodes">The memcached nodes defined in the configuration.</param>
		void Initialize(IList<MemcachedNode> nodes);
		/// <summary>
		/// Returns the memcached node the specified key belongs to.
		/// </summary>
		/// <param name="key">The key of the item to be located.</param>
		/// <returns>The <see cref="T:MemcachedNode"/> the specifed item belongs to</returns>
		MemcachedNode Locate(string key);
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