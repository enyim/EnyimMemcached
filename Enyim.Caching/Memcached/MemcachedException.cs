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
	/// The exception that is thrown when an unknown error occures in the <see cref="T:MemcachedClient"/>
	/// </summary>
	[global::System.Serializable]
	public class MemcachedException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:MemcachedException"/> class.
		/// </summary>
		public MemcachedException() { }
		/// <summary>
		/// Initializes a new instance of the <see cref="T:MemcachedException"/> class with a specified error message.
		/// </summary>
		public MemcachedException(string message) : base(message) { }
		/// <summary>
		/// Initializes a new instance of the <see cref="T:MemcachedException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
		/// </summary>
		public MemcachedException(string message, Exception inner) : base(message, inner) { }
		/// <summary>
		/// Initializes a new instance of the <see cref="T:MemcachedException"/> class with serialized data.
		/// </summary>
		protected MemcachedException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context)
			: base(info, context) { }
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