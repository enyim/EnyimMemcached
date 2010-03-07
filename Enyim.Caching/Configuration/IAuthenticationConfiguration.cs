using System;
using System.Collections.Generic;

namespace Enyim.Caching.Configuration
{
	/// <summary>
	/// Defines an interface for configuring the authentication paramaters the <see cref="T:MemcachedClient"/>.
	/// </summary>
	public interface IAuthenticationConfiguration
	{
		/// <summary>
		/// Gets or sets the type of the <see cref="T:Enyim.Caching.Memcached.IAuthenticationProvider"/> which will be used authehticate the connections to the Memcached nodes.
		/// </summary>
		Type Type { get; set; }

		/// <summary>
		/// Gets or sets the parameters passed to the authenticator instance.
		/// </summary>
		Dictionary<string, object> Parameters { get; }
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