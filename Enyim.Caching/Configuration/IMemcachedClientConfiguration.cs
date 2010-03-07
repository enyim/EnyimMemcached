using System;
using System.Collections.Generic;
using System.Net;
using Enyim.Caching.Memcached;

namespace Enyim.Caching.Configuration
{
	/// <summary>
	/// Defines an interface for configuring the <see cref="T:MemcachedClient"/>.
	/// </summary>
	public interface IMemcachedClientConfiguration
	{
		/// <summary>
		/// Gets a list of <see cref="T:IPEndPoint"/> each representing a Memcached server in the pool.
		/// </summary>
		IList<IPEndPoint> Servers { get; }

		/// <summary>
		/// Gets the configuration of the socket pool.
		/// </summary>
		ISocketPoolConfiguration SocketPool { get; }

		/// <summary>
		/// Gets the authentication settings.
		/// </summary>
		IAuthenticationConfiguration Authentication { get; }

		/// <summary>
		/// Gets or sets the type of the <see cref="T:Enyim.Caching.Memcached.IMemcachedKeyTransformer"/> which will be used to convert item keys for Memcached.
		/// </summary>
		Type KeyTransformer { get; set; }

		/// <summary>
		/// Gets or sets the type of the <see cref="T:Enyim.Caching.Memcached.IMemcachedNodeLocator"/> which will be used to assign items to Memcached nodes.
		/// </summary>
		Type NodeLocator { get; set; }

		/// <summary>
		/// Gets or sets the type of the <see cref="T:Enyim.Caching.Memcached.ITranscoder"/> which will be used serialzie or deserialize items.
		/// </summary>
		Type Transcoder { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the client reports its activity to perfmon.
		/// </summary>
		bool EnablePerformanceCounters { get; set; }

		/// <summary>
		/// Gets or sets the type of the communication between client and server.
		/// </summary>
		MemcachedProtocol Protocol { get; set; }
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