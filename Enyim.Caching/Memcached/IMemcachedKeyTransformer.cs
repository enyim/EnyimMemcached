using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace Enyim.Caching.Memcached
{
	/// <summary>
	/// Converts Memcached item keys into a custom format.
	/// </summary>
	public interface IMemcachedKeyTransformer
	{
		/// <summary>
		/// Performs the transformation.
		/// </summary>
		/// <param name="key">The key to be transformed.</param>
		/// <returns>the transformed key.</returns>
		string Transform(string key);
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