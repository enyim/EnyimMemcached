using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;
using System.Threading;
using System.Net;

namespace NorthScale.Store
{
	internal class BucketConfigListener : MessageStreamListener, IDisposable
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(BucketConfigListener));

		private ConfigHelper helper;
		private string bucketName;
		private int previousHash;
		private ManualResetEvent initEvent;

		public BucketConfigListener(string bucketName, Uri[] poolUrls)
			: base(poolUrls)
		{
			this.bucketName = bucketName ?? "default";
		}

		public event Action<ClusterConfig> ClusterConfigChanged;

		protected override void OnMessageReceived(string message)
		{
			base.OnMessageReceived(message);

			// everything failed
			if (String.IsNullOrEmpty(message))
			{
				// only signal a config change when the last config was not empty
				if (this.previousHash != Int32.MinValue)
				{
					this.previousHash = Int32.MinValue;

					this.RaiseConfigChanged(null);
				}

				return;
			}

			// deserialize the buckets
			var jss = new JavaScriptSerializer();
			var config = jss.Deserialize<ClusterConfig>(message);

			var hc = config.GetHashCode();
			if (hc == this.previousHash)
				return;

			this.previousHash = hc;

			this.RaiseConfigChanged(config);
		}

		protected override Uri ResolveUri(Uri uri)
		{
			try
			{
				var bucket = this.helper.ResolveBucket(uri, this.bucketName);

				var firstNode = bucket.nodes.FirstOrDefault();

				// quick fix for membase beta2
				if (firstNode != null && firstNode.version == "1.6.0beta2")
					return new Uri(uri, bucket.streamingUri.Replace("/bucketsStreaming/", "/bucketsStreamingConfig/"));

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
			this.initEvent = new ManualResetEvent(false);
			this.helper = new ConfigHelper()
			{
				Timeout = this.Timeout,
				Credentials = this.Credentials
			};

			base.Start();

			using (this.initEvent) this.initEvent.WaitOne();
			this.initEvent = null;
		}

		private void RaiseConfigChanged(ClusterConfig config)
		{
			var ccc = this.ClusterConfigChanged;

			// we got a new config, notify the pool to reload itself
			if (ccc != null)
				ccc(config);

			// trigger the event so Start stops blocking
			if (this.initEvent != null)
				this.initEvent.Set();
		}

		void IDisposable.Dispose()
		{
			if (this.helper != null)
			{
				((IDisposable)this.helper).Dispose();
				this.helper = null;
			}
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
