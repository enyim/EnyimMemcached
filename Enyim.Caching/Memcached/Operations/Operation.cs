using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace Enyim.Caching.Memcached
{
	internal abstract class Operation : IDisposable
	{
		private static log4net.ILog log = log4net.LogManager.GetLogger(typeof(Operation));

		private bool isDisposed;
		private bool success;
		private ServerPool serverPool;

		protected Operation(ServerPool serverPool)
		{
			this.serverPool = serverPool;
		}

		public void Execute()
		{
			this.success = false;

			try
			{
				if (this.CheckDisposed(false))
					return;

				this.success = this.ExecuteAction();
			}
			catch (NotSupportedException)
			{
				throw;
			}
			catch (Exception e)
			{
				// TODO generic catch-all does not seem to be a good idea now. Some errors (like command not supported by server) should be exposed while retaining the fire-and-forget behavior
				log.Error(e);
			}
		}

		protected ServerPool ServerPool
		{
			get { return this.serverPool; }
		}

		protected abstract bool ExecuteAction();

		protected bool CheckDisposed(bool throwOnError)
		{
			if (throwOnError && this.isDisposed)
				throw new ObjectDisposedException("Operation");

			return this.isDisposed;
		}

		public bool Success
		{
			get { return this.success; }
		}

		#region [ IDisposable                  ]
		public virtual void Dispose()
		{
			this.isDisposed = true;
		}

		void IDisposable.Dispose()
		{
			this.Dispose();
		}
		#endregion
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