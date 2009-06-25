using System;
using System.Collections.Generic;
using System.Text;

namespace Enyim.Caching.Memcached
{
	/// <summary>
	/// Represent a stat item returned by Memcached.
	/// </summary>
	public enum StatItem : int
	{
		/// <summary>
		/// The number of seconds the server has been running.
		/// </summary>
		Uptime = 0,
		/// <summary>
		/// Current time according to the server.
		/// </summary>
		ServerTime,
		/// <summary>
		/// The version of the server.
		/// </summary>
		Version,
		/// <summary>
		/// The number of items stored by the server.
		/// </summary>
		ItemCount,
		/// <summary>
		/// The total number of items stored by the server including the ones whihc have been already evicted.
		/// </summary>
		TotalItems,
		/// <summary>
		/// Number of active connections to the server.
		/// </summary>
		ConnectionCount,
		/// <summary>
		/// The total number of connections ever made to the server.
		/// </summary>
		TotalConnections,
		/// <summary>
		/// ?
		/// </summary>
		ConnectionStructures,

		/// <summary>
		/// Number of get operations performed by the server.
		/// </summary>
		GetCount,
		/// <summary>
		/// Number of set operations performed by the server.
		/// </summary>
		SetCount,
		/// <summary>
		/// Cache hit.
		/// </summary>
		GetHits,
		/// <summary>
		/// Cache miss.
		/// </summary>
		GetMisses,

		/// <summary>
		/// ?
		/// </summary>
		UsedBytes,
		/// <summary>
		/// Number of bytes read from the server.
		/// </summary>
		BytesRead,
		/// <summary>
		/// Number of bytes written to the server.
		/// </summary>
		BytesWritten,
		/// <summary>
		/// ?
		/// </summary>
		MaxBytes
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