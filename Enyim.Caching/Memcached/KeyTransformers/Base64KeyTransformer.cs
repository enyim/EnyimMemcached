using System;
using System.Text;

namespace Enyim.Caching.Memcached
{
	/// <summary>
	/// A key transformer which converts the item keys into Base64.
	/// </summary>
	public class Base64KeyTransformer : KeyTransformerBase
	{
		public override string Transform(string key)
		{
			return Convert.ToBase64String(Encoding.UTF8.GetBytes(key), Base64FormattingOptions.None);
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