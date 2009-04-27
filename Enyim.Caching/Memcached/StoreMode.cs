using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Security.Cryptography;
using System.Globalization;
using System.Threading;

namespace Enyim.Caching.Memcached
{
	/// <summary>
	/// Inidicates the mode how the items are stored in Memcached.
	/// </summary>
	public enum StoreMode
	{
		/// <summary>
		/// Store the data, but only if the server does not already hold data for a given key
		/// </summary>
		Add,
		/// <summary>
		/// Store the data, but only if the server does already hold data for a given key
		/// </summary>
		Replace,
		/// <summary>
		/// Store the data, overwrite if already exist
		/// </summary>
		Set
	};

	internal enum StoreCommand
	{
		/// <summary>
		/// Store the data, but only if the server does not already hold data for a given key
		/// </summary>
		Add,
		/// <summary>
		/// Store the data, but only if the server does already hold data for a given key
		/// </summary>
		Replace,
		/// <summary>
		/// Store the data, overwrite if already exist
		/// </summary>
		Set,
		/// <summary>
		/// Appends the data to an existing key's data
		/// </summary>
		Append,
		/// <summary>
		/// Inserts the data before an existing key's data
		/// </summary>
		Prepend,
		/// <summary>
		/// Stores the data only if it has not been updated by someone else. Uses a "transaction id" to check for modification.
		/// </summary>
		CheckAndSet
	};
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