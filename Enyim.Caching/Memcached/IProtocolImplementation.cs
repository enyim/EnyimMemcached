using System;
using System.Collections.Generic;

namespace Enyim.Caching.Memcached
{
	internal interface IProtocolImplementation : IDisposable
	{
		object Get(string key);
		bool TryGet(string key, out object value);
		IDictionary<string, object> Get(IEnumerable<string> keys);

		bool Store(StoreMode mode, string key, object value, uint expiration);
		bool Remove(string key);
		ulong Mutate(MutationMode mode, string key, ulong startValue, ulong step, uint expiration);
		bool Concatenate(ConcatenationMode mode, string key, ArraySegment<byte> data);

		void FlushAll();
		ServerStats Stats();

		IAuthenticator CreateAuthenticator(ISaslAuthenticationProvider provider);
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
