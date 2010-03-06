using System;

namespace Enyim.Caching.Memcached
{
	/// <summary>
	/// The exception that is thrown when a client error occures during communicating with the Memcached servers.
	/// </summary>
	[global::System.Serializable]
	public class MemcachedClientException : MemcachedException
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:MemcachedClientException"/> class.
		/// </summary>
		public MemcachedClientException() { }
		/// <summary>
		/// Initializes a new instance of the <see cref="T:MemcachedClientException"/> class with a specified error message.
		/// </summary>
		public MemcachedClientException(string message) : base(message) { }
		/// <summary>
		/// Initializes a new instance of the <see cref="T:MemcachedClientException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
		/// </summary>
		public MemcachedClientException(string message, Exception inner) : base(message, inner) { }
		/// <summary>
		/// Initializes a new instance of the <see cref="T:MemcachedClientException"/> class with serialized data.
		/// </summary>
		protected MemcachedClientException(
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