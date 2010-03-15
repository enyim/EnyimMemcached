using System.Collections.Generic;

namespace NorthScale.Store
{
#pragma warning disable 649
	class Bucket
	{
		public string name;

		public string uri;
		public string streamingUri;

		public BucketNode[] nodes;
	}

	class BucketNode
	{
		public string hostname;
		public string status;
		public BucketNodePorts ports;

		public override string ToString()
		{
			return this.hostname + ":" + this.ports.ToString() + "," + this.status;
		}

		public override int GetHashCode()
		{
			return this.ToString().GetHashCode();
		}

		internal static readonly IEqualityComparer<BucketNode> ComparerInstance = new Comparer();

		#region [ Comparer                     ]
		private class Comparer : IEqualityComparer<BucketNode>
		{
			bool IEqualityComparer<BucketNode>.Equals(BucketNode x, BucketNode y)
			{
				return x.hostname == y.hostname
						&& x.ports.direct == y.ports.direct
						&& x.ports.proxy == y.ports.proxy
						&& x.status == y.status;
			}

			int IEqualityComparer<BucketNode>.GetHashCode(BucketNode obj)
			{
				return obj.GetHashCode();
			}
		}
		#endregion
	}

	class BucketNodePorts
	{
		public int direct;
		public int proxy;

		public override string ToString()
		{
			return this.direct + "|" + this.proxy;
		}

		public override int GetHashCode()
		{
			return this.ToString().GetHashCode();
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
