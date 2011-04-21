using System;
using System.Collections.Generic;

namespace Enyim.Caching.Memcached.Protocol
{
	/// <summary>
	/// Base class for implementing operations.
	/// </summary>
	public abstract class Operation : IOperation
	{
		private static readonly Enyim.Caching.ILog log = Enyim.Caching.LogManager.GetLogger(typeof(Operation));

		protected Operation() { }

		internal protected abstract IList<ArraySegment<byte>> GetBuffer();
		internal protected abstract bool ReadResponse(PooledSocket socket);

		//internal protected abstract IAsyncResult BeginReadResponse(PooledSocket socket);
		//internal protected abstract bool EndReadResponse(IAsyncResult result);

		IList<ArraySegment<byte>> IOperation.GetBuffer()
		{
			return this.GetBuffer();
		}

		bool IOperation.ReadResponse(PooledSocket socket)
		{
			return this.ReadResponse(socket);
		}

		//IAsyncResult IOperation.BeginReadResponse(PooledSocket socket)
		//{
		//    return this.BeginReadResponse(socket);
		//}

		//bool IOperation.EndReadResponse(IAsyncResult result)
		//{
		//    return this.EndReadResponse(result);
		//}
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
