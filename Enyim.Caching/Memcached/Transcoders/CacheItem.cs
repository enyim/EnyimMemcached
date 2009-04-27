using System;
using System.Collections.Generic;
using System.Text;

namespace Enyim.Caching.Memcached
{
	/// <summary>
	/// Represents an object either being retrieved from the cache
	/// or being sent to the cache.
	/// </summary>
	public struct CacheItem
	{
		private ArraySegment<byte> data;
		private ushort flags;

		/// <summary>
		/// Initializes a new instance of <see cref="T:CacheItem"/>.
		/// </summary>
		/// <param name="flags">Custom item data.</param>
		/// <param name="data">The serialized item.</param>
		public CacheItem(ushort flags, ArraySegment<byte> data)
		{
			this.data = data;
			this.flags = flags;
		}

		/// <summary>
		/// The data representing the item being stored/retireved.
		/// </summary>
		public ArraySegment<byte> Data
		{
			get { return this.data; }
			set { this.data = value; }
		}

		/// <summary>
		/// Flags set for this instance.
		/// </summary>
		public ushort Flag
		{
			get { return this.flags; }
			set { this.flags = value; }
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