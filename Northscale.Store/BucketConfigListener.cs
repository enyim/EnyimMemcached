using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;
using System.Threading;

namespace NorthScale.Store
{
	internal class BucketConfigListener : MessageStreamListener
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(BucketConfigListener));

		private ConfigHelper helper;
		private string bucketName;

		public BucketConfigListener(string bucketName, ConfigHelper helper, Uri[] poolUrls)
			: base(poolUrls)
		{
			this.helper = helper;
			this.bucketName = bucketName ?? "default";

			this.Credentials = helper.Credentials;
			this.Timeout = helper.Timeout;
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

		private ManualResetEvent mre;

		protected override Uri ResolveUri(Uri uri)
		{
			try
			{
				var bucket = this.helper.ResolveBucket(uri, this.bucketName);

				return new Uri(uri, bucket.streamingUri);
			}
			catch (Exception e)
			{
				log.Error("Error resolving streaming uri", e);

				return null;
			}
		}

		public override void Start()
		{
			this.mre = new ManualResetEvent(false);

			base.Start();

			using (this.mre) this.mre.WaitOne();
			this.mre = null;
		}

		private void RaiseNodeListChanged(IEnumerable<BucketNode> nodes)
		{
			var nlc = this.NodeListChanged;

			// we got a new config, notify the pool to reload itself
			if (nlc != null)
				nlc(nodes);

			// trigger the event so Start stops blocking
			if (this.mre != null)
				this.mre.Set();
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
