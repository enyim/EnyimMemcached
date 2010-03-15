using System;
using System.Linq;
using System.Collections.Generic;

namespace Enyim.Caching.Memcached.Operations.Binary
{
	internal class MultiGetOperation : Operation
	{
		private IEnumerable<string> keys;
		private Dictionary<string, object> result;

		public MultiGetOperation(IServerPool pool, IEnumerable<string> keys)
			: base(pool)
		{
			this.keys = keys;
		}

		public Dictionary<string, object> Result
		{
			get { return this.result; }
		}

		protected override bool ExecuteAction()
		{
			// 1. map each key to a node
			// 2. build itemCount * GetQ buffer, and close it with NoOp to get the responses
			// 3. read the response of each node
			// 4. merge the responses into a dictionary

			// map each key to the appropriate server in the pool
			var splitKeys = this.SplitKeys(this.keys);

			// we'll open 1 socket for each server
			var mgets = new List<MGetSession>(splitKeys.Count);

			var idmap = new Dictionary<string, int>();

			foreach (var group in splitKeys)
			{
				// HACK this will transform the keys again, we should precalculate them and pass to the getter
				var mg = new MGetSession(this.ServerPool, group.Key, group.Value);

				try
				{
					if (mg.Write()) mgets.Add(mg);
					mg = null;
				}
				finally
				{
					if (mg != null) ((IDisposable)mg).Dispose();
				}
			}

			var retval = new Dictionary<string, object>();

			// process each response and build a dictionary from the results
			foreach (var mg in mgets)
				using (mg)
				{
					var results = mg.Read();
					foreach (var de in results)
						retval.Add(de.Key, de.Value);
				}

			this.result = retval;

			return true;
		}

		#region [ MGetSession                  ]
		/// <summary>
		/// Handles the MultiGet against a node
		/// </summary>
		private class MGetSession : IDisposable
		{
			private IServerPool pool;
			private IMemcachedNode node;
			private List<string> keys;
			private PooledSocket socket;

			public MGetSession(IServerPool pool, IMemcachedNode node, List<string> keys)
			{
				this.pool = pool;
				this.node = node;
				this.keys = keys;
			}

			private Dictionary<int, string> requestedItemMap = new Dictionary<int, string>();
			private int lastId;

			public bool Write()
			{
				if (!this.node.IsAlive) return false;

				this.socket = this.node.Acquire();
				
				// exit early if the node is dead
				if (this.socket == null || !this.socket.IsAlive) return false;

				var transformer = this.pool.KeyTransformer;
				var buffers = new List<ArraySegment<byte>>();

				// build a GetQ for each key
				foreach (string realKey in this.keys)
				{
					string hashedKey = transformer.Transform(realKey);

					var request = new BinaryRequest(OpCode.GetQ);
					request.Key = hashedKey;

					// store the request's id so later we can find 
					// out whihc response is which item
					// this way we do not have to use GetKQ
					// (whihc sends back the item's key with the data)
					requestedItemMap[request.CorrelationId] = realKey;
					buffers.AddRange(request.CreateBuffer());
				}

				// noop forces the server to send the responses of the 
				// previous quiet commands
				var noop = new BinaryRequest(OpCode.NoOp);

				// noop always succeeds so we'll read until we get the noop's response
				this.lastId = noop.CorrelationId;
				buffers.AddRange(noop.CreateBuffer());

				try
				{
					this.socket.Write(buffers);

					// if the write failed the Read() will be skipped
					return this.socket.IsAlive;
				}
				catch
				{
					// write error most probably
					return false;
				}
			}

			public IDictionary<string, object> Read()
			{
				var response = new BinaryResponse();
				var retval = new Dictionary<string, object>();
				var transcoder = this.pool.Transcoder;

				try
				{
					while (true)
					{
						// if nothing else, the noop will succeed
						if (response.Read(this.socket))
						{
							// found the noop, quit
							if (response.CorrelationId == this.lastId) return retval;

							string key;

							// find the key to the response
							if (!this.requestedItemMap.TryGetValue(response.CorrelationId, out key))
							{
								// we're not supposed to get here tho
								log.WarnFormat("Found response with CorrelationId {0}, but no key is matching it.", response.CorrelationId);
								continue;
							}

							if (log.IsDebugEnabled) log.DebugFormat("Reading item {0}", key);

							// deserialize the response
							int flags = BinaryConverter.DecodeInt32(response.Extra, 0);
							retval[key] = transcoder.Deserialize(new CacheItem((ushort)flags, response.Data));
						}
					}
				}
				catch
				{
					// read failed, return the items we've read so far
					return retval;
				}
			}

			void IDisposable.Dispose()
			{
				GC.SuppressFinalize(this);

				if (this.socket == null) return;

				try
				{
					((IDisposable)this.socket).Dispose();
					this.socket = null;
				}
				catch
				{ }
			}

			private static log4net.ILog log = log4net.LogManager.GetLogger(typeof(MGetSession));

		}
		#endregion
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
