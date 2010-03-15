using System;
using System.Collections.Generic;
using System.Text;

namespace Enyim.Caching.Memcached.Operations.Binary
{
	internal class ConcatenationOperation : ItemOperation
	{
		private ArraySegment<byte> data;
		private ConcatenationMode mode;

		public ConcatenationOperation(IServerPool pool, ConcatenationMode mode, string key, ArraySegment<byte> data)
			: base(pool, key)
		{
			this.data = data;
			this.mode = mode;
		}

		protected override bool ExecuteAction()
		{
			PooledSocket socket = this.Socket;
			if (socket == null) return false;

			BinaryRequest request = new BinaryRequest(this.mode == ConcatenationMode.Append ? OpCode.Append : OpCode.Prepend);
			request.Key = this.Key;
			request.Data = this.data;

			request.Write(socket);

			BinaryResponse response = new BinaryResponse();
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
