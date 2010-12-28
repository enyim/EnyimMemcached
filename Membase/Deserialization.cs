using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;

namespace Membase
{
#pragma warning disable 649
	internal class ClusterInfo
	{
		public string name;
		public ClusterNode[] nodes;
		public ClusterBucketInfo buckets;
	}

	internal class ClusterBucketInfo
	{
		public string uri;
	}

	internal class ClusterConfig
	{
		public string name;

		public string uri;
		public string streamingUri;

		public ClusterNode[] nodes;

		public VBucketConfig vBucketServerMap;
		public VBucketConfig vBucketForwardServerMap;

		// mecached|membase
		public string bucketType;
		// sasl
		public string authType;
		// password for the bucket
		public string saslPassword;

		public override int GetHashCode()
		{
			var cnehc = new Enyim.HashCodeCombiner();

			for (var i = 0; i < nodes.Length; i++)
				cnehc.Add(nodes[i].GetHashCode());

			if (vBucketForwardServerMap != null)
				cnehc.Add(vBucketForwardServerMap.GetHashCode());

			if (vBucketServerMap != null)
				cnehc.Add(vBucketServerMap.GetHashCode());

			cnehc.Add(this.name.GetHashCode());
			cnehc.Add(this.streamingUri.GetHashCode());

			return cnehc.CurrentHash;
		}
	}

	internal class VBucketConfig
	{
		public string hashAlgorithm;
		public int numReplicas;
		public string[] serverList;
		public int[][] vBucketMap;

		public override int GetHashCode()
		{
			var ehc = new Enyim.HashCodeCombiner(this.hashAlgorithm.GetHashCode());
			ehc.Add(this.numReplicas);

			for (var i = 0; i < this.serverList.Length; i++)
				ehc.Add(this.serverList[i].GetHashCode());

			for (var i = 0; i < vBucketMap.Length; i++)
			{
				var ehc2 = new Enyim.HashCodeCombiner();
				var tmp = vBucketMap[i];

				for (var j = 0; j < tmp.Length; j++)
					ehc2.Add(tmp[j]);

				ehc.Add(ehc2.CurrentHash);
			}

			return ehc.CurrentHash;
		}
	}

	internal class ClusterNode
	{
		private string _hostname;

		public string hostname
		{
			get { return this._hostname; }
			set
			{
				var tmp = value;

				// strip the management port (mc server 1.0.3> & membase 1.6>)
				if (!String.IsNullOrEmpty(tmp))
				{
					var index = tmp.IndexOf(':');
					if (index > 0)
						tmp = tmp.Substring(0, index);
				}

				this._hostname = tmp;
			}
		}

		public string status;
		public ClusterNodePorts ports;

		public string version;

		public override int GetHashCode()
		{
			return Enyim.HashCodeCombiner.Combine(this._hostname.GetHashCode(), this.status.GetHashCode(), ports.GetHashCode());
		}

		public static readonly IEqualityComparer<ClusterNode> ComparerInstance = new Comparer();

		#region [ Comparer                     ]
		private class Comparer : IEqualityComparer<ClusterNode>
		{
			bool IEqualityComparer<ClusterNode>.Equals(ClusterNode x, ClusterNode y)
			{
				return x.hostname == y.hostname
						&& x.ports.direct == y.ports.direct
						&& x.ports.proxy == y.ports.proxy
						&& x.status == y.status;
			}

			int IEqualityComparer<ClusterNode>.GetHashCode(ClusterNode obj)
			{
				return obj.GetHashCode();
			}
		}
		#endregion
	}

	internal class ClusterNodePorts
	{
		public int direct;
		public int proxy;

		public override string ToString()
		{
			return this.direct + "|" + this.proxy;
		}

		public override int GetHashCode()
		{
			return Enyim.HashCodeCombiner.Combine(this.direct, this.proxy);
		}
	}
#pragma warning restore 649
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
