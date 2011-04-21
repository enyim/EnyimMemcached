using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enyim.Caching.Memcached.Protocol.Binary;
using Enyim.Caching.Memcached;
using System.IO;
using System.Threading;
using Enyim.Caching;

namespace Membase
{
	[Flags]
	public enum SyncMode { Mutation = 1, Persistence = 2, Replication = 4 };

	internal class SyncOperation : BinaryOperation, ISyncOperation
	{
		private VBucketNodeLocator locator;
		private KeyValuePair<string, ulong>[] keys;
		private uint flags;

		public SyncOperation(VBucketNodeLocator locator, KeyValuePair<string, ulong>[] keys, SyncMode mode, int replicationCount)
		{
			if (keys == null) throw new ArgumentNullException("keys");
			if (keys.Length > 0xffff) throw new ArgumentException("Only 0xffff items are supported");

			this.flags = GetFlags(mode, replicationCount);

			this.locator = locator;
			this.keys = keys;
		}

		public SyncResult[] Result { get; private set; }

		private static uint GetFlags(SyncMode mode, int replicationCount)
		{
			#region [ Flag definitions             ]
			/*
				Size	Field
				4	rep count
				1	persist flag
				1	Mutation flag
				1	and/or for rep+persist
				Replication count: 4 bits. Block until has sent this many replicas (16 replicas ought to be enough for anybody).

				Persistence count:	1 bit. If 1, block until persisted.
				Mutation flag:		If 1, block while the key’s CAS is valid.
				And/Or flag:		If 0 and a replica count and persistence flag are both given, block until either condition is satisfied,
									else block until both conditions are satisfied.

				Flags layout (32-bit)
				16			8			4	1	1	1	1
				RESERVED	RESERVED	R	P	M	R+P	RESERVED
				R = Replication count
				P = Persistence count
				M = Observe mutation events
				R+P = replica + persistence operation
			*/
			#endregion

			if (replicationCount > 16 || replicationCount < 0) throw new ArgumentOutOfRangeException("replicationCount", "<= 0 replicationCount <= 16!");

			uint retval = (uint)(replicationCount << 4);

			var hasRepl = (mode & SyncMode.Replication) == SyncMode.Replication && replicationCount > 0;
			var hasPers = (mode & SyncMode.Persistence) == SyncMode.Persistence;
			var hasMut = (mode & SyncMode.Mutation) == SyncMode.Mutation;

			if (hasMut) retval |= 4;
			if (hasPers) retval |= 8;
			if (hasRepl && hasPers) retval |= 2;

			return retval;
		}

		protected override BinaryRequest Build()
		{
			var request = new BinaryRequest(0x96)
			{
				Data = this.BuildBody()
			};

			return request;
		}

		protected unsafe ArraySegment<byte> BuildBody()
		{
			var header = new byte[6];

			// 0-3 flags
			// 4-5 item count
			BinaryConverter.EncodeUInt32(this.flags, header, 0);
			BinaryConverter.EncodeUInt16((ushort)this.keys.Length, header, 4);

			var ms = new MemoryStream();
			ms.Write(header, 0, header.Length);

			var itemHeader = new byte[8 + 2 + 2];

			fixed (byte* p = itemHeader)
			{
				//  0- 7: cas
				//  8- 9: vbucket
				// 10-11: key length \ repeat
				// 12- N: key        /

				for (var i = 0; i < this.keys.Length; i++)
				{
					var keySpec = this.keys[i];
					var itemKey = Encoding.UTF8.GetBytes(keySpec.Key);

					// cas
					BinaryConverter.EncodeUInt64(keySpec.Value, p, 0);
					// vbucket
					BinaryConverter.EncodeUInt16((ushort)this.locator.GetIndex(keySpec.Key), p, 8);
					// key length
					BinaryConverter.EncodeUInt16((ushort)itemKey.Length, p, 10);

					ms.Write(itemHeader, 0, 12);
					ms.Write(itemKey, 0, itemKey.Length);
				}
			}

			return new ArraySegment<byte>(ms.GetBuffer(), 0, (int)ms.Length);
		}

		protected override bool ReadResponse(PooledSocket socket)
		{
			var response = new BinaryResponse();
			if (response.Read(socket))
			{
				this.Result = DecodeResult(response.Data);

				return true;
			}

			return false;
		}

		private unsafe SyncResult[] DecodeResult(ArraySegment<byte> result)
		{
			var data = result.Array;

			fixed (byte* p = data)
			{
				var offset = result.Offset;
				var count = BinaryConverter.DecodeUInt16(p, offset);
				var retval = new SyncResult[count];
				offset += 2;

				for (var i = 0; i < retval.Length; i++)
				{
					var cas = BinaryConverter.DecodeUInt64(p, offset);
					// skip vbucket (8-9)
					var keyLength = BinaryConverter.DecodeUInt16(p, offset + 10);
					var eventId = (SyncEvent)p[offset + 12];
					var key = Encoding.UTF8.GetString(data, offset + 13, keyLength);

					retval[i] = new SyncResult
					{
						Cas = cas,
						Event = eventId,
						Key = key
					};

					offset += (13 + keyLength);
				}

				return retval;
			}
		}

		SyncResult[] ISyncOperation.Result
		{
			get { return this.Result; }
		}
	}

	public class SyncResult
	{
		public string Key { get; internal set; }
		public ulong Cas { get; internal set; }
		public SyncEvent Event { get; internal set; }
	}

	public enum SyncEvent { Unknown = 0, Persisted, Modified, Replicated, Deleted, InvalidKey, InvalidCas }

	public interface ISyncOperation : IOperation
	{
		SyncResult[] Result { get; }
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
