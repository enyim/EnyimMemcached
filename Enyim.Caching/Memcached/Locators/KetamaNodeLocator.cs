using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Threading;

namespace Enyim.Caching.Memcached
{
	/// <summary>
	/// Implements Ketama cosistent hashing, compatible with the "spymemcached" Java client
	/// </summary>
	public sealed class KetamaNodeLocator : IMemcachedNodeLocator
	{
		// TODO make this configurable without restructuring the whole config system
		private const string HashName = "System.Security.Cryptography.MD5";
		private const int ServerAddressMutations = 160;
		private LookupData lookupData;

		void IMemcachedNodeLocator.Initialize(IList<IMemcachedNode> nodes)
		{
			// sizeof(uint)
			const int KeyLength = 4;
			var hashAlgo = HashAlgorithm.Create(HashName);

			int PartCount = hashAlgo.HashSize / 8 / KeyLength; // HashSize is in bits, uint is 4 bytes long
			if (PartCount < 1) throw new ArgumentOutOfRangeException("The hash algorithm must provide at least 32 bits long hashes");

			var keys = new List<uint>(nodes.Count * KetamaNodeLocator.ServerAddressMutations);
			var keyToServer = new Dictionary<uint, IMemcachedNode>(keys.Count, new UIntEqualityComparer());

			for (int nodeIndex = 0; nodeIndex < nodes.Count; nodeIndex++)
			{
				var currentNode = nodes[nodeIndex];

				// every server is registered numberOfKeys times
				// using UInt32s generated from the different parts of the hash
				// i.e. hash is 64 bit:
				// 01 02 03 04 05 06 07
				// server will be stored with keys 0x07060504 & 0x03020100
				string address = currentNode.EndPoint.ToString();

				for (int mutation = 0; mutation < ServerAddressMutations / PartCount; mutation++)
				{
					byte[] data = hashAlgo.ComputeHash(Encoding.ASCII.GetBytes(address + "-" + mutation));

					for (int p = 0; p < PartCount; p++)
					{
						var tmp = p * 4;
						var key = ((uint)data[tmp + 3] << 24)
									| ((uint)data[tmp + 2] << 16)
									| ((uint)data[tmp + 1] << 8)
									| ((uint)data[tmp]);

						keys.Add(key);
						keyToServer[key] = currentNode;
					}
				}
			}

			keys.Sort();

			var lookupData = new LookupData
			{
				keys = keys.ToArray(),
				keyToServer = keyToServer,
				servers = nodes.ToArray()
			};

			// now the re-initialization is kinda thread safe without locking
			Interlocked.Exchange(ref this.lookupData, lookupData);
		}

		private uint GetKeyHash(string key)
		{
			var hashAlgo = HashAlgorithm.Create(HashName);
			var data = hashAlgo.ComputeHash(Encoding.UTF8.GetBytes(key));

			return ((uint)data[3] << 24) | ((uint)data[2] << 16) | ((uint)data[1] << 8) | ((uint)data[0]);
		}

		IMemcachedNode IMemcachedNodeLocator.Locate(string key)
		{
			if (key == null) throw new ArgumentNullException("key");

			var ld = this.lookupData;

			switch (ld.servers.Length)
			{
				case 0: return null;
				case 1: return ld.servers[0];
			}

			var retval = LocateNode(ld, this.GetKeyHash(key));

			// isalive is not atomic
			if (!retval.IsAlive)
			{
				for (var i = 0; i < ld.servers.Length; i++)
				{
					// -- this is from spymemcached so we select the same node for the same items
					ulong tmpKey = (ulong)GetKeyHash(i + key);
					tmpKey += (uint)(tmpKey ^ (tmpKey >> 32));
					tmpKey &= 0xffffffffL; /* truncate to 32-bits */
					// -- end

					retval = LocateNode(ld, (uint)tmpKey);

					if (retval.IsAlive) return retval;
				}
			}

			return retval.IsAlive ? retval : null;
		}

		IEnumerable<IMemcachedNode> IMemcachedNodeLocator.GetWorkingNodes()
		{
			var ld = this.lookupData;

			if (ld.servers == null || ld.servers.Length == 0)
				return Enumerable.Empty<IMemcachedNode>();

			var retval = new IMemcachedNode[ld.servers.Length];
			Array.Copy(ld.servers, retval, retval.Length);

			return retval;
		}

		private static IMemcachedNode LocateNode(LookupData ld, uint itemKeyHash)
		{
			// get the index of the server assigned to this hash
			int foundIndex = Array.BinarySearch<uint>(ld.keys, itemKeyHash);

			// no exact match
			if (foundIndex < 0)
			{
				// this is the nearest server in the list
				foundIndex = ~foundIndex;

				if (foundIndex == 0)
				{
					// it's smaller than everything, so use the last server (with the highest key)
					foundIndex = ld.keys.Length - 1;
				}
				else if (foundIndex >= ld.keys.Length)
				{
					// the key was larger than all server keys, so return the first server
					foundIndex = 0;
				}
			}

			if (foundIndex < 0 || foundIndex > ld.keys.Length)
				return null;

			return ld.keyToServer[ld.keys[foundIndex]];
		}

		#region [ LookupData                   ]
		// this will encapsulate all the indexes we need for lookup
		// so the instance can be reinitialized without locking
		// in case an IMemcachedConfig implementation returns the same instance form the CreateLocator()
		private class LookupData
		{
			public IMemcachedNode[] servers;
			// holds all server keys for mapping an item key to the server consistently
			public uint[] keys;
			// used to lookup a server based on its key
			public Dictionary<uint, IMemcachedNode> keyToServer;
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
