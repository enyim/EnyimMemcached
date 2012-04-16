using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enyim.Caching.Memcached.Results;

namespace Enyim.Caching.Memcached
{
	/// <summary>
	/// Interface for API methods that return detailed operation results
	/// </summary>
	public interface IMemcachedResultsClient
	{
		IStoreOperationResult ExecuteStore(StoreMode mode, string key, object value);
		IStoreOperationResult ExecuteStore(StoreMode mode, string key, object value, DateTime expiresAt);
		IStoreOperationResult ExecuteStore(StoreMode mode, string key, object value, TimeSpan validFor);
	}
}

#region [ License information          ]
/* ************************************************************
 * 
 *    @author Couchbase <info@couchbase.com>
 *    @copyright 2012 Couchbase, Inc.
 *    @copyright 2012 Attila Kiskó, enyim.com
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