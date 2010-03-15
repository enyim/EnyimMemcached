using System;
using System.Collections.Generic;
using System.Text;

namespace Enyim.Caching.Memcached
{
	/// <summary>
	/// This is a ketama-like consistent hashing based node locator. Used when no other <see cref="T:IMemcachedNodeLocator"/> is specified for the pool.
	/// </summary>
	public sealed class DefaultNodeLocator : IMemcachedNodeLocator
	{
		private const int ServerAddressMutations = 100;

		// holds all server keys for mapping an item key to the server consistently
		private uint[] keys;
		// used to lookup a server based on its key
		private Dictionary<uint, IMemcachedNode> servers = new Dictionary<uint, IMemcachedNode>(new UIntEqualityComparer());
		private bool isInitialized;
		private object initLock = new Object();

		void IMemcachedNodeLocator.Initialize(IList<IMemcachedNode> nodes)
		{
			if (this.isInitialized)
				throw new InvalidOperationException("Instance is already initialized.");

			// locking on this is rude but easy
			lock (this.initLock)
			{
				if (this.isInitialized)
					throw new InvalidOperationException("Instance is already initialized.");

				this.keys = new uint[nodes.Count * DefaultNodeLocator.ServerAddressMutations];

				int nodeIdx = 0;

				foreach (IMemcachedNode node in nodes)
				{
					List<uint> tmpKeys = DefaultNodeLocator.GenerateKeys(node, DefaultNodeLocator.ServerAddressMutations);

					tmpKeys.ForEach(delegate(uint k)
					{
						this.servers[k] = node;
					});

					tmpKeys.CopyTo(this.keys, nodeIdx);
					nodeIdx += DefaultNodeLocator.ServerAddressMutations;
				}

				Array.Sort<uint>(this.keys);

				this.isInitialized = true;
			}
		}

		IMemcachedNode IMemcachedNodeLocator.Locate(string key)
		{
			if (!this.isInitialized)
				throw new InvalidOperationException("You must call Initialize first");

			if (key == null)
				throw new ArgumentNullException("key");

			if (this.keys.Length == 0)
				return null;

			uint itemKeyHash = BitConverter.ToUInt32(new FNV1a().ComputeHash(Encoding.UTF8.GetBytes(key)), 0);
			// get the index of the server assigned to this hash
			int foundIndex = Array.BinarySearch<uint>(this.keys, itemKeyHash);

			// no exact match
			if (foundIndex < 0)
			{
				// this is the nearest server in the list
				foundIndex = ~foundIndex;

				if (foundIndex == 0)
				{
					// it's smaller than everything, so use the last server (with the highest key)
					foundIndex = this.keys.Length - 1;
				}
				else if (foundIndex >= this.keys.Length)
				{
					// the key was larger than all server keys, so return the first server
					foundIndex = 0;
				}
			}

			if (foundIndex < 0 || foundIndex > this.keys.Length)
				return null;

			return this.servers[this.keys[foundIndex]];
		}

		private static List<uint> GenerateKeys(IMemcachedNode node, int numberOfKeys)
		{
			const int KeyLength = 4;
			const int PartCount = 1; // (ModifiedFNV.HashSize / 8) / KeyLength; // HashSize is in bits, uint is 4 byte long

			//if (partCount < 1)
			//    throw new ArgumentOutOfRangeException("The hash algorithm must provide at least 32 bits long hashes");

			List<uint> k = new List<uint>(PartCount * numberOfKeys);

			// every server is registered numberOfKeys times
			// using UInt32s generated from the different parts of the hash
			// i.e. hash is 64 bit:
			// 00 00 aa bb 00 00 cc dd
			// server will be stored with keys 0x0000aabb & 0x0000ccdd
			// (or a bit differently based on the little/big indianness of the host)
			string address = node.EndPoint.ToString();

			for (int i = 0; i < numberOfKeys; i++)
			{
				byte[] data = new FNV1a().ComputeHash(Encoding.ASCII.GetBytes(String.Concat(address, "-", i)));

				for (int h = 0; h < PartCount; h++)
				{
					k.Add(BitConverter.ToUInt32(data, h * KeyLength));
				}
			}

			return k;
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
