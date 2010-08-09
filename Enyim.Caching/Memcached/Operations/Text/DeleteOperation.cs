using System;

namespace Enyim.Caching.Memcached.Operations.Text
{
	internal sealed class DeleteOperation : ItemOperation
	{
		internal DeleteOperation(IServerPool pool, string key)
			: base(pool, key)
		{
		}

		protected override bool ExecuteAction()
		{
			PooledSocket socket = this.Socket;
			if (socket == null)
				return false;

			TextSocketHelper.SendCommand(socket, "delete " + this.HashedKey);

			return String.Compare(TextSocketHelper.ReadResponse(socket), "DELETED", StringComparison.Ordinal) == 0;
		}

		protected override System.Collections.Generic.IList<ArraySegment<byte>> GetBuffer()
		{
			throw new NotImplementedException();
		}

		protected override bool ReadResponse(PooledSocket socket)
		{
			throw new NotImplementedException();
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
