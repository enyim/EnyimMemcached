using System;
using System.Collections.Generic;

namespace Enyim.Caching.Memcached
{
	/// <summary>
	/// This is a simple node locator with no computation overhead, always returns the first server from the list. Use only in single server deployments.
	/// </summary>
	public sealed class SingleNodeLocator : IMemcachedNodeLocator
	{
		private MemcachedNode node;
		private bool isInitialized;

		void IMemcachedNodeLocator.Initialize(IList<MemcachedNode> nodes)
		{
			if (this.isInitialized)
				throw new InvalidOperationException("Instance is already initialized.");

			// locking on this is rude but easy
			lock (this)
			{
				if (this.isInitialized)
					throw new InvalidOperationException("Instance is already initialized.");

				if (nodes.Count > 0)
					node = nodes[0];

				this.isInitialized = true;
			}
		}

		MemcachedNode IMemcachedNodeLocator.Locate(string key)
		{
			if (!this.isInitialized)
				throw new InvalidOperationException("You must call Initialize first");

			return this.node;
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