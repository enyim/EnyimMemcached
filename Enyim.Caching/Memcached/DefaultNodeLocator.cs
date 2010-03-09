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
		private const int ServerAddressMutations = 160;

		// holds all server keys for mapping an item key to the server consistently
		private uint[] keys;
		// used to lookup a server based on its key
		private Dictionary<uint, MemcachedNode> servers = new Dictionary<uint, MemcachedNode>(new UIntEqualityComparer());
		private bool isInitialized;
		private object initLock = new Object();

		void IMemcachedNodeLocator.Initialize(IList<MemcachedNode> nodes)
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

				foreach (MemcachedNode node in nodes)
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

		MemcachedNode IMemcachedNodeLocator.Locate(string key)
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

		private static List<uint> GenerateKeys(MemcachedNode node, int numberOfKeys)
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
 * Copyright (c) Attila Kiskó, enyim.com
 *
 * This source code is subject to terms and conditions of 
 * Microsoft Permissive License (Ms-PL).
 * 
 * A copy of the license can be found in the License.html
 * file at the root of this distribution. If you can not 
 * locate the License, please send an email to a@enyim.com
 * 
 * By using this source code in any fashion, you are 
 * agreeing to be bound by the terms of the Microsoft 
 * Permissive License.
 *
 * You must not remove this notice, or any other, from this
 * software.
 *
 * ************************************************************/
#endregion