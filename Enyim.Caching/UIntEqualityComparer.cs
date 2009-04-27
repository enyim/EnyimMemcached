using System;
using System.Collections.Generic;
using System.Text;

namespace Enyim.Caching
{
	/// <summary>
	/// A fast comparer for dictionaries indexed by UInt. Faster than using Comparer.Default
	/// </summary>
	public sealed class UIntEqualityComparer : IEqualityComparer<uint>
	{
		bool IEqualityComparer<uint>.Equals(uint x, uint y)
		{
			return x == y;
		}

		int IEqualityComparer<uint>.GetHashCode(uint value)
		{
			return value.GetHashCode();
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