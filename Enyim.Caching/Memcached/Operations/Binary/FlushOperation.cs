using System;
using System.Collections.Generic;

namespace Enyim.Caching.Memcached.Operations.Binary
{
	internal class FlushOperation : Operation, IFlushOperation
	{
		public FlushOperation() { }

		protected internal override IList<ArraySegment<byte>> GetBuffer()
		{
			var request = new BinaryRequest(OpCode.Flush);

			return request.CreateBuffer();
		}

		protected internal override bool ReadResponse(PooledSocket socket)
		{
			var response = new BinaryResponse();

			return response.Read(socket);
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
