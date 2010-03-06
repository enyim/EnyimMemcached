using System;

namespace Enyim.Caching.Configuration
{
	/// <summary>
	/// Defines an interface for configuring the socket pool for the <see cref="T:MemcachedClient"/>.
	/// </summary>
	public interface ISocketPoolConfiguration
	{
		/// <summary>
		/// Gets or sets a value indicating the minimum amount of sockets per server in the socket pool.
		/// </summary>
		/// <returns>The minimum amount of sockets per server in the socket pool.</returns>
		int MinPoolSize
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets a value indicating the maximum amount of sockets per server in the socket pool.
		/// </summary>
		/// <returns>The maximum amount of sockets per server in the socket pool.</returns>
		int MaxPoolSize
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets a value that specifies the amount of time after which the connection attempt will fail.
		/// </summary>
		/// <returns>The value of the connection timeout.</returns>
		TimeSpan ConnectionTimeout
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets a value that specifies the amount of time after which receiving data from the socket will fail.
		/// </summary>
		/// <returns>The value of the receive timeout.</returns>
		TimeSpan ReceiveTimeout
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets a value that specifies the amount of time after which an unresponsive (dead) server will be checked if it is working.
		/// </summary>
		/// <returns>The value of the dead timeout.</returns>
		TimeSpan DeadTimeout
		{
			get;
			set;
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