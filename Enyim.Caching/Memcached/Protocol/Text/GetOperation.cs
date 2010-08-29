
namespace Enyim.Caching.Memcached.Protocol.Text
{
	public class GetOperation : SingleItemOperation, IGetOperation
	{
		private CacheItem result;

		internal GetOperation(string key) : base(key) { }

		protected internal override System.Collections.Generic.IList<System.ArraySegment<byte>> GetBuffer()
		{
			var command = "gets " + this.Key + TextSocketHelper.CommandTerminator;

			return TextSocketHelper.GetCommandBuffer(command);
		}

		protected internal override bool ReadResponse(PooledSocket socket)
		{
			GetResponse r = GetHelper.ReadItem(socket);

			if (r == null) return false;

			this.result = r.Item;
			this.Cas = r.CasValue;

			GetHelper.FinishCurrent(socket);

			return true;
		}

		CacheItem IGetOperation.Result
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
