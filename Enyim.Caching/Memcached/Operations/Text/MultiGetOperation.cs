using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace Enyim.Caching.Memcached.Operations.Text
{
	internal class MultiGetOperation : MultiItemOperation, IMultiGetOperation
	{
		private static log4net.ILog log = log4net.LogManager.GetLogger(typeof(MultiGetOperation));

		private Dictionary<string, CacheItem> result;
		private Dictionary<string, ulong> casValues;

		public MultiGetOperation(IList<string> keys) : base(keys) { }

		public IDictionary<string, ulong> CasValues
		{
			get { return this.casValues; }
		}

		protected internal override IList<ArraySegment<byte>> GetBuffer()
		{
			// gets key1 key2 key3 ... keyN\r\n
			var commandBuilder = new StringBuilder("gets");

			foreach (var key in this.Keys)
				commandBuilder.Append(" ").Append(key);

			commandBuilder.Append(TextSocketHelper.CommandTerminator);

			return TextSocketHelper.GetCommandBuffer(commandBuilder.ToString());
		}

		protected internal override bool ReadResponse(PooledSocket socket)
		{
			var retval = new Dictionary<string, CacheItem>();
			var cas = new Dictionary<string, ulong>();

			try
			{
				GetResponse r;

				while ((r = GetHelper.ReadItem(socket)) != null)
				{
					var key = r.Key;

					retval[key] = r.Item;
					cas[key] = r.CasValue;
				}
			}
			catch (NotSupportedException)
			{
				throw;
			}
			catch (Exception e)
			{
				log.Error(e);
			}

			this.result = retval;
			this.casValues = cas;

			return true;
		}

		Dictionary<string, CacheItem> IMultiGetOperation.Result
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
