using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Enyim.Caching.Tests
{
	public class MemcachedClientMutateTests : MemcachedClientTestsBase
	{
		[Fact]
		public void When_Incrementing_Value_Result_Is_Successful()
		{
			var key = GetUniqueKey("mutate");
			var mutateResult = _client.ExecuteIncrement(key, 100, 10);
			MutateAssertPass(mutateResult, 100);

			mutateResult = _client.ExecuteIncrement(key, 100, 10);
			MutateAssertPass(mutateResult, 110);
		}

		[Fact]
		public void When_Decrementing_Value_Result_Is_Successful()
		{
			var key = GetUniqueKey("mutate");
			var mutateResult = _client.ExecuteDecrement(key, 100, 10);
			MutateAssertPass(mutateResult, 100);

			mutateResult = _client.ExecuteDecrement(key, 100, 10);
			MutateAssertPass(mutateResult, 90);
		}
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