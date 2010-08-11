using System;
using System.Collections.Generic;

namespace Enyim.Caching.Memcached.Protocol.Text
{
	public class TextOperationFactory : IOperationFactory
	{
		IGetOperation IOperationFactory.Get(string key)
		{
			return new GetOperation(key);
		}

		IMultiGetOperation IOperationFactory.MultiGet(IList<string> keys)
		{
			return new MultiGetOperation(keys);
		}

		IStoreOperation IOperationFactory.Store(StoreMode mode, string key, CacheItem value, uint expires)
		{
			return new StoreOperation(mode, key, value, expires, 0);
		}

		IDeleteOperation IOperationFactory.Delete(string key)
		{
			return new DeleteOperation(key);
		}

		IMutatorOperation IOperationFactory.Mutate(MutationMode mode, string key, ulong defaultValue, ulong delta, uint expires)
		{
			return new MutatorOperation(mode, key, delta);
		}

		IConcatOperation IOperationFactory.Concat(ConcatenationMode mode, string key, ArraySegment<byte> data)
		{
			return new ConcateOperation(mode, key, data);
		}

		IStatsOperation IOperationFactory.Stats()
		{
			return new StatsOperation();
		}

		IFlushOperation IOperationFactory.Flush()
		{
			return new FlushOperation();
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
