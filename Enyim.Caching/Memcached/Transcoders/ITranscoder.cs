using System;
using System.Collections.Generic;
using System.Text;

namespace Enyim.Caching.Memcached
{
	/// <summary>
	/// Provides an interface for serializing items for Memcached.
	/// </summary>
	public interface ITranscoder
	{
		/// <summary>
		/// Serializes an object for storing in the cache.
		/// </summary>
		/// <param name="o">The object to serialize</param>
		/// <returns>The serialized object</returns>
		CacheItem Serialize(Object o);

		/// <summary>
		/// Deserializes the <see cref="T:CacheItem"/> into an object.
		/// </summary>
		/// <param name="item">The stream that contains the data to deserialize.</param>
		/// <returns>The deserialized object</returns>
		object Deserialize(CacheItem item);
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