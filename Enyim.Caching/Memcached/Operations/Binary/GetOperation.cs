
namespace Enyim.Caching.Memcached.Operations.Binary
{
	internal class GetOperation : ItemOperation
	{
		public GetOperation(IServerPool pool, string key) : base(pool, key) { }

		protected override bool ExecuteAction()
		{
			PooledSocket socket = this.Socket;
			if (socket == null) return false;

			BinaryRequest request = new BinaryRequest(OpCode.Get);
			request.Reserved = (ushort)socket.OwnerNode.Bucket;
			request.Key = this.HashedKey;
			request.Write(socket);

			BinaryResponse response = new BinaryResponse();

			if (response.Read(socket))
			{
				int flags = BinaryConverter.DecodeInt32(response.Extra, 0);
				this.result = this.ServerPool.Transcoder.Deserialize(new CacheItem((ushort)flags, response.Data));

				return true;
			}

			return false;
		}

		private object result;

		public object Result
		{
			get { return this.result; }
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
