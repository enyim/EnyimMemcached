using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;

namespace NorthScale.Store
{
	internal class BucketConfigListener : MessageStreamListener
	{
		public BucketConfigListener(Uri[] urls, IEnumerable<BucketNode> currentNodes)
			: base(urls)
		{
			if (currentNodes != null)
				this.lastNodes = currentNodes.ToList();
		}

		private List<BucketNode> lastNodes;

		public event Action<IEnumerable<BucketNode>> NodeListChanged;

		protected override void OnMessageReceived(string message)
		{
			base.OnMessageReceived(message);

			// everything failed
			if (String.IsNullOrEmpty(message)) 
			{
				if (this.lastNodes != null)
					this.RaiseNodeListChanged(Enumerable.Empty<BucketNode>());

				this.lastNodes = null;
				return;
			}

			// deserialize the buckets
			var jss = new JavaScriptSerializer();
			var config = jss.Deserialize<Bucket>(message);

			// ignore the unhealthy nodes
			var newNodes = (from node in config.nodes
							where node.status == "healthy"
							orderby node.hostname
							select node).ToList();

			// check if the new config is the same as the last one
			if (this.lastNodes != null
				&& this.lastNodes.SequenceEqual(newNodes, BucketNode.ComparerInstance))
				return;

			this.lastNodes = newNodes;
			this.RaiseNodeListChanged(newNodes);
		}

		private void RaiseNodeListChanged(IEnumerable<BucketNode> nodes)
		{
			var nlc = this.NodeListChanged;

			// we got a new config, notify the pool to reload itself
			if (nlc != null)
				nlc(nodes);
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
