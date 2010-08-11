using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enyim.Caching.Memcached.Protocol.Binary;
using Enyim.Caching.Memcached;

namespace NorthScale.Store
{
	internal class VBucketAwareOperationFactory : IOperationFactory
	{
		IGetOperation IOperationFactory.Get(string key)
		{
			return new VBGet(key);
		}

		IMultiGetOperation IOperationFactory.MultiGet(IList<string> keys)
		{
			return new VBMget(keys);
		}

		IStoreOperation IOperationFactory.Store(StoreMode mode, string key, CacheItem value, uint expires)
		{
			return new VBStore(mode, key, value, expires);
		}

		IDeleteOperation IOperationFactory.Delete(string key)
		{
			return new VBDelete(key);
		}

		IMutatorOperation IOperationFactory.Mutate(MutationMode mode, string key, ulong defaultValue, ulong delta, uint expires)
		{
			return new VBMutator(mode, key, defaultValue, delta, expires);
		}

		IConcatOperation IOperationFactory.Concat(ConcatenationMode mode, string key, ArraySegment<byte> data)
		{
			return new VBConcat(mode, key, data);
		}

		IStatsOperation IOperationFactory.Stats()
		{
			return new StatsOperation();
		}

		IFlushOperation IOperationFactory.Flush()
		{
			return new FlushOperation();
		}

		#region [ Custom operations            ]

		private class VBStore : StoreOperation, IVBucketAwareOperation
		{
			public VBStore(StoreMode mode, string key, CacheItem value, uint expires) : base(mode, key, value, expires) { }

			ushort IVBucketAwareOperation.Index { get; set; }

			protected override BinaryRequest Build()
			{
				var retval = base.Build();
				retval.Reserved = ((IVBucketAwareOperation)this).Index;

				return retval;
			}
		}

		private class VBDelete : DeleteOperation, IVBucketAwareOperation
		{
			public VBDelete(string key) : base(key) { }

			ushort IVBucketAwareOperation.Index { get; set; }

			protected override BinaryRequest Build()
			{
				var retval = base.Build();
				retval.Reserved = ((IVBucketAwareOperation)this).Index;

				return retval;
			}
		}

		private class VBMutator : MutatorOperation, IVBucketAwareOperation
		{
			public VBMutator(MutationMode mode, string key, ulong defaultValue, ulong delta, uint expires)
				: base(mode, key, defaultValue, delta, expires) { }

			ushort IVBucketAwareOperation.Index { get; set; }

			protected override BinaryRequest Build()
			{
				var retval = base.Build();
				retval.Reserved = ((IVBucketAwareOperation)this).Index;

				return retval;
			}
		}

		private class VBMget : MultiGetOperation, IVBucketAwareOperation
		{
			ushort IVBucketAwareOperation.Index { get; set; }

			public VBMget(IList<string> keys) : base(keys) { }

			protected override BinaryRequest Build(string key)
			{
				var retval = base.Build(key);
				retval.Reserved = ((IVBucketAwareOperation)this).Index;

				return retval;
			}
		}

		private class VBConcat : ConcatOperation, IVBucketAwareOperation
		{
			public VBConcat(ConcatenationMode mode, string key, ArraySegment<byte> data) : base(mode, key, data) { }

			ushort IVBucketAwareOperation.Index { get; set; }

			protected override BinaryRequest Build()
			{
				var retval = base.Build();
				retval.Reserved = ((IVBucketAwareOperation)this).Index;

				return retval;
			}
		}

		private class VBGet : GetOperation, IVBucketAwareOperation
		{
			public VBGet(string key) : base(key) { }

			ushort IVBucketAwareOperation.Index { get; set; }

			protected override BinaryRequest Build()
			{
				var retval = base.Build();
				retval.Reserved = ((IVBucketAwareOperation)this).Index;

				return retval;
			}
		}

		#endregion
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
