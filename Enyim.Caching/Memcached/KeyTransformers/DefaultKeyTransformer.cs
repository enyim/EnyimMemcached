using System;
using System.Collections.Generic;
using System.Text;

namespace Enyim.Caching.Memcached
{
	public class DefaultKeyTransformer : KeyTransformerBase
	{
		static readonly char[] ForbiddenChars = { 
			'\u0000', '\u0001', '\u0002', '\u0003',
			'\u0004', '\u0005', '\u0006', '\u0007',
			'\u0008', '\u0009', '\u000a', '\u000b',
			'\u000c', '\u000d', '\u000e', '\u000f',
			'\u0010', '\u0011', '\u0012', '\u0013',
			'\u0014', '\u0015', '\u0016', '\u0017',
			'\u0018', '\u0019', '\u001a', '\u001b',
			'\u001c', '\u001d', '\u001e', '\u001f',
			'\u0020'
		};

		public override string Transform(string key)
		{
			// TODO we should convert it to UTf8 byte stream then check that for the forbidden byte values
			if (key.IndexOfAny(ForbiddenChars) > -1)
				throw new ArgumentException("Keys cannot contain the chars 0x00-0x02f and space.");

			return key;
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