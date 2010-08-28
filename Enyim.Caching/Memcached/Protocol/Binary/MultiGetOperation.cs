using System;
using System.Linq;
using System.Collections.Generic;

namespace Enyim.Caching.Memcached.Protocol.Binary
{
	public class MultiGetOperation : BinaryMultiItemOperation, IMultiGetOperation
	{
		private static log4net.ILog log = log4net.LogManager.GetLogger(typeof(MultiGetOperation));

		private Dictionary<string, CacheItem> result;
		private Dictionary<int, string> idToKey;
		private int noopId;

		public MultiGetOperation(IList<string> keys) : base(keys) { }

		protected override BinaryRequest Build(string key)
		{
			var request = new BinaryRequest(OpCode.GetQ)
			{
				Key = key
			};

			return request;
		}

		protected internal override IList<ArraySegment<byte>> GetBuffer()
		{
			var keys = this.Keys;

			if (keys == null || keys.Count == 0)
			{
				if (log.IsWarnEnabled) log.Warn("Empty multiget!");

				return new ArraySegment<byte>[0];
			}

			if (log.IsDebugEnabled)
				log.DebugFormat("Building multi-get for {0} keys", keys.Count);

			// map the command's correlationId to the item key,
			// so we can use GetQ (which only returns the item data)
			this.idToKey = new Dictionary<int, string>();

			// get ops have 2 segments, header + key
			var buffers = new List<ArraySegment<byte>>(keys.Count * 2);

			foreach (var key in keys)
			{
				var request = this.Build(key);

				request.CreateBuffer(buffers);

				// we use this to map the responses to the keys
				idToKey[request.CorrelationId] = key;
			}

			// uncork the server
			var noop = new BinaryRequest(OpCode.NoOp);
			this.noopId = noop.CorrelationId;

			noop.CreateBuffer(buffers);

			return buffers;
		}

		protected internal override bool ReadResponse(PooledSocket socket)
		{
			this.result = new Dictionary<string, CacheItem>();
			var response = new BinaryResponse();

			while (response.Read(socket))
			{
				// found the noop, quit
				if (response.CorrelationId == this.noopId)
					return true;

				string key;

				// find the key to the response
				if (!this.idToKey.TryGetValue(response.CorrelationId, out key))
				{
					// we're not supposed to get here tho
					log.WarnFormat("Found response with CorrelationId {0}, but no key is matching it.", response.CorrelationId);
					continue;
				}

				if (log.IsDebugEnabled) log.DebugFormat("Reading item {0}", key);

				// deserialize the response
				int flags = BinaryConverter.DecodeInt32(response.Extra, 0);
				this.result[key] = new CacheItem((ushort)flags, response.Data);
			}

			// finished reading but we did not find the NOOP
			return false;
		}

		public Dictionary<string, CacheItem> Result
		{
			get { return this.result; }
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
