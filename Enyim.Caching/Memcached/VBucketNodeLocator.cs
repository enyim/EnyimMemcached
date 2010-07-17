using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Net;
using System.Web.Script.Serialization;
using System.Collections.ObjectModel;
using System.Web;
using Enyim.Caching.Configuration;
using System.Configuration;
using System.IO;

namespace Enyim.Caching.Memcached
{
	/// <summary>
	/// Implements a vbucket based node locator.
	/// </summary>
	public class VBucketNodeLocator : IMemcachedNodeLocator
	{
		private IVBucketConfiguration config;
		private int mask;

		public VBucketNodeLocator(IVBucketConfiguration config)
		{
			this.config = config;

			var log = Math.Log(this.config.Buckets.Count, 2);
			if (log != (int)log)
				throw new ArgumentException("Buckets.Count must be a power of 2!");

			this.mask = config.Buckets.Count - 1;
		}

		[ThreadStatic]
		private static HashAlgorithm currentAlgo;

		private HashAlgorithm GetAlgo()
		{
			// we cache the HashAlgorithm instance per thread
			// they are reinitialized before every ComputeHash but we avoid creating then GCing them every time we need 
			// to find something (which will happen a lot)
			// we cannot use ThreadStatic for asp.net (requests can change threads) so the hasher will be recreated for every request
			// (we could use an object pool but talk about overkill)
			var ctx = HttpContext.Current;
			if (ctx == null)
				return currentAlgo ?? (currentAlgo = this.config.CreateHashAlgorithm());

			var algo = ctx.Items["**VBucket.CurrentAlgo"] as HashAlgorithm;
			if (algo == null)
				ctx.Items["**VBucket.CurrentAlgo"] = algo = this.config.CreateHashAlgorithm();

			return algo;
		}

		#region [ IMemcachedNodeLocator        ]

		private IMemcachedNode[] nodes;

		void IMemcachedNodeLocator.Initialize(IList<IMemcachedNode> nodes)
		{
			this.nodes = nodes.ToArray();
		}

		IMemcachedNode IMemcachedNodeLocator.Locate(string key)
		{
			var ha = this.GetAlgo();

			//little shortcut for some hashes; we skip the uint -> byte[] -> uint conversion
			var iuha = ha as IUIntHashAlgorithm;
			var keyBytes = Encoding.UTF8.GetBytes(key);

			uint keyHash = (iuha == null)
							? keyHash = BitConverter.ToUInt32(ha.ComputeHash(keyBytes), 0)
							: iuha.ComputeHash(keyBytes);

			int index = (int)(keyHash & this.mask);

			return this.nodes[this.config.Buckets[index].Master];
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
