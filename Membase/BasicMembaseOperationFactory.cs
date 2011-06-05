using System;
using System.Collections.Generic;

namespace Membase
{
	internal class BasicMembaseOperationFactory : Enyim.Caching.Memcached.Protocol.Binary.BinaryOperationFactory, IMembaseOperationFactory
	{
		internal static readonly BasicMembaseOperationFactory Instance = new BasicMembaseOperationFactory();

		ITouchOperation IMembaseOperationFactory.Touch(string key, uint newExpiration)
		{
			return new TouchOperation(null, key, newExpiration);
		}

		IGetAndTouchOperation IMembaseOperationFactory.GetAndTouch(string key, uint newExpiration)
		{
			return new GetAndTouchOperation(null, key, newExpiration);
		}

		ISyncOperation IMembaseOperationFactory.Sync(SyncMode mode, IList<KeyValuePair<string, ulong>> keys, int replicationCount)
		{
			throw new NotSupportedException("Sync is not supported on memcached buckets.");
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
