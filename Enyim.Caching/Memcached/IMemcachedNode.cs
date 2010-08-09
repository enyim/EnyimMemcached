using System;
using System.Net;
using System.Collections.Generic;
using Enyim.Caching.Memcached.Operations;

namespace Enyim.Caching.Memcached
{
	internal abstract class ItemOperation2 : Operation, IItemOperation
	{
		protected ItemOperation2(string key)
		{
			this.Key = key;
		}

		public string Key { get; private set; }

		string IItemOperation.Key
		{
			get { return this.Key; }
		}
	}

	internal abstract class MultiItemOperation2 : Operation, IMultiItemOperation
	{
		public MultiItemOperation2(IList<string> keys)
		{
			this.Keys = keys;
		}

		public IList<string> Keys { get; private set; }
		IList<string> IMultiItemOperation.Keys { get { return this.Keys; } }
	}

	public interface IMemcachedNode : IDisposable
	{
		IPEndPoint EndPoint { get; }
		bool IsAlive { get; }
		bool Ping();
		PooledSocket Acquire();


		// TEMP HACK
		int Bucket { get; }

		bool Execute(IOperation op);

		IAsyncResult BeginExecute(IOperation op, AsyncCallback callback, object state);
		bool EndExecute(IAsyncResult result);
	}

	//interface IRequest
	//{
	//    IList<ArraySegment<byte>> GetBuffer();

	//    IAsyncResult BeginReadResponse(PooledSocket socket, AsyncCallback callback, object state);
	//    bool EndReadResponse(IAsyncResult result);
	//}

	public interface IOperation
	{
		IList<ArraySegment<byte>> GetBuffer();
		bool ReadResponse(PooledSocket socket);
	}

	public interface IOpFactory
	{
		IGetOperation Get(string key);
		IMultiGetOperation MultiGet(IList<string> keys);
		IStoreOperation Store(StoreMode mode, string key, CacheItem value, uint expires);
		IDeleteOperation Delete(string key);
		IMutatorOperation Mutate(MutationMode mode, string key, ulong defaultValue, ulong delta, uint expires);
		IConcatOperation Concat(ConcatenationMode mode, string key, ArraySegment<byte> data);
		IStatsOperation Stats();
		IFlushOperation Flush();
	}

	public interface IConcatOperation : IOperation
	{
		ConcatenationMode Mode { get; }
	}

	public interface IStatsOperation : IOperation
	{
		Dictionary<string, string> Result { get; }
	}

	public interface IMutatorOperation : IItemOperation
	{
		MutationMode Mode { get; }
		ulong Result { get; }
	}

	public interface IGetOperation : IItemOperation
	{
		CacheItem Result { get; }
	}

	public interface IMultiGetOperation : IOperation
	{
		Dictionary<string, CacheItem> Result { get; }
	}

	public interface IDeleteOperation : IItemOperation
	{
	}

	public interface IStoreOperation : IItemOperation
	{
		StoreMode Mode { get; }
	}

	public interface IFlushOperation : IOperation
	{
	}

	public interface IItemOperation : IOperation
	{
		string Key { get; }
	}

	public interface IMultiItemOperation : IOperation
	{
		IList<string> Keys { get; }
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
