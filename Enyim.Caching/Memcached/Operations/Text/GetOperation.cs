
namespace Enyim.Caching.Memcached.Operations.Text
{
	internal class GetOperation : ItemOperation
	{
		private object result;

		internal GetOperation(IServerPool pool, string key)
			: base(pool, key)
		{
		}

		public object Result
		{
			get { return this.result; }
		}

		protected override bool ExecuteAction()
		{
			PooledSocket socket = this.Socket;

			if (socket == null)
				return false;

			TextSocketHelper.SendCommand(socket, "get " + this.HashedKey);

			GetResponse r = GetHelper.ReadItem(socket);

			if (r == null) return false;

			this.result = this.ServerPool.Transcoder.Deserialize(r.Item);
			GetHelper.FinishCurrent(socket);

			return true;
		}

		protected override System.Collections.Generic.IList<System.ArraySegment<byte>> GetBuffer()
		{
			throw new System.NotImplementedException();
		}

		protected override bool ReadResponse(PooledSocket socket)
		{
			throw new System.NotImplementedException();
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
