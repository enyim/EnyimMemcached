using System;
using System.Net;
using System.Collections.Generic;
using Enyim.Caching.Memcached.Operations;

namespace Enyim.Caching.Memcached
{
	public interface IOperation
	{
		IList<ArraySegment<byte>> GetBuffer();
		bool ReadResponse(PooledSocket socket);
	}

	public interface ISingleItemOperation : IOperation
	{
		string Key { get; }
	}

	public interface IMultiItemOperation : IOperation
	{
		IList<string> Keys { get; }
	}

	public interface IGetOperation : ISingleItemOperation
	{
		CacheItem Result { get; }
	}

	public interface IMultiGetOperation : IOperation
	{
		Dictionary<string, CacheItem> Result { get; }
	}

	public interface IStoreOperation : ISingleItemOperation
	{
		StoreMode Mode { get; }
	}

	public interface IDeleteOperation : ISingleItemOperation
	{
	}

	public interface IConcatOperation : IOperation
	{
		ConcatenationMode Mode { get; }
	}

	public interface IMutatorOperation : ISingleItemOperation
	{
		MutationMode Mode { get; }
		ulong Result { get; }
	}

	public interface IStatsOperation : IOperation
	{
		Dictionary<string, string> Result { get; }
	}

	public interface IFlushOperation : IOperation
	{
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
