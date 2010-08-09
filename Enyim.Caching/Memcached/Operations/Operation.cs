using System;
using System.Collections.Generic;

namespace Enyim.Caching.Memcached.Operations
{
	/// <summary>
	/// Base class for implementing operations.
	/// </summary>
	public abstract class Operation : IDisposable, IOperation
	{
		private static log4net.ILog log = log4net.LogManager.GetLogger(typeof(Operation));

		private bool isDisposed;
		private IServerPool serverPool;

		protected Operation() { }

		protected Operation(IServerPool serverPool)
		{
			this.serverPool = serverPool;
		}

		public bool Execute(PooledSocket ps)
		{
			return false;
		}

		public bool Execute()
		{
			try
			{
				if (this.CheckDisposed(false)) return false;

				return this.ExecuteAction();
			}
			catch (NotSupportedException)
			{
				throw;
			}
			catch (Exception e)
			{
				// TODO generic catch-all does not seem to be a good idea now. Some errors (like command not supported by server) should be exposed while retaining the fire-and-forget behavior
				log.Error(e);

				return false;
			}
		}

		protected IServerPool ServerPool
		{
			get { return this.serverPool; }
		}

		protected virtual bool ExecuteAction() { return false; }

		protected bool CheckDisposed(bool throwOnError)
		{
			if (throwOnError && this.isDisposed)
				throw new ObjectDisposedException("Operation");

			return this.isDisposed;
		}

		///// <summary>
		///// Maps each key in the list to a MemcachedNode
		///// </summary>
		///// <param name="keys"></param>
		///// <returns></returns>
		//protected Dictionary<IMemcachedNode, List<string>> SplitKeys(IEnumerable<string> keys)
		//{
		//    var retval = new Dictionary<IMemcachedNode, List<string>>(MemcachedNode.Comparer.Instance);
		//    var kt = this.serverPool.KeyTransformer;
		//    var locator = this.serverPool.NodeLocator;

		//    foreach (var key in keys)
		//    {
		//        var node = locator.Locate(kt.Transform(key));
		//        if (node != null)
		//        {
		//            List<string> list;

		//            if (!retval.TryGetValue(node, out list))
		//                retval[node] = list = new List<string>();

		//            list.Add(key);
		//        }
		//    }

		//    return retval;
		//}

		~Operation()
		{
			try { ((IDisposable)this).Dispose(); }
			catch { }
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

		protected abstract IList<ArraySegment<byte>> GetBuffer();
		protected abstract bool ReadResponse(PooledSocket socket);

		IList<ArraySegment<byte>> IOperation.GetBuffer()
		{
			return this.GetBuffer();
		}

		bool IOperation.ReadResponse(PooledSocket socket)
		{
			return this.ReadResponse(socket);
		}
	}
}

#region [ License information          ]
/* ************************************************************
 * 
 *    Copyright (c) 2010 Attila Kiskó, enyim.com
 *    
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *    
 *        http://www.apache.org/licenses/LICENSE-2.0
 *    
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *    
 * ************************************************************/
#endregion
