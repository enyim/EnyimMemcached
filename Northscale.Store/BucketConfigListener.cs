using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;
using System.Threading;
using System.Net;
using Enyim;

namespace NorthScale.Store
{
	internal class BucketConfigListener
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(BucketConfigListener));

		private Uri[] poolUrls;
		private string bucketName;
		private NetworkCredential credential;

		public BucketConfigListener(Uri[] poolUrls, string bucketName, NetworkCredential credential)
		{
			this.poolUrls = poolUrls;
			this.bucketName = bucketName ?? "default";

			this.credential = credential;

			this.Timeout = 10000;
			this.DeadTimeout = 20000;
		}

		public int Timeout { get; set; }
		public int DeadTimeout { get; set; }

		#region listener cache
		static Dictionary<int, MessageStreamListener> listeners = new Dictionary<int, MessageStreamListener>();

		private MessageStreamListener GetPooledListener()
		{
			// create a unique key based on the parameters
			// to find out if we already have a listener attached to this pool
			var hcc = new HashCodeCombiner();

			hcc.Add(this.Timeout);
			hcc.Add(this.DeadTimeout);
			hcc.Add(this.bucketName.GetHashCode());

			if (credential != null)
			{
				hcc.Add((this.credential.UserName ?? String.Empty).GetHashCode());
				hcc.Add((this.credential.Password ?? String.Empty).GetHashCode());
				hcc.Add((this.credential.Domain ?? String.Empty).GetHashCode());
			}

			for (var i = 0; i < this.poolUrls.Length; i++)
				hcc.Add(this.poolUrls[i].GetHashCode());

			var hash = hcc.CurrentHash;

			MessageStreamListener retval;

			if (!listeners.TryGetValue(hash, out retval))
				lock (listeners)
					if (!listeners.TryGetValue(hash, out retval))
					{
						listeners[hash] = retval = new MessageStreamListener(poolUrls, this.ResolveBucketUri);
						retval.Timeout = this.Timeout;
						retval.DeadTimeout = this.DeadTimeout;
						retval.Credentials = this.credential;

						retval.Start();
					}

			retval.Subscribe(this.HandleMessage);

			return retval;
		}

		#endregion

		private Uri ResolveBucketUri(WebClientWithTimeout client, Uri uri)
		{
			try
			{
				var helper = new ConfigHelper(client);
				var bucket = helper.ResolveBucket(uri, this.bucketName);

				return new Uri(uri, bucket.streamingUri);
			}
			catch (Exception e)
			{
				log.Error("Error resolving streaming uri", e);

				return null;
			}
		}

		public event Action<ClusterConfig> ClusterConfigChanged;

		private void HandleMessage(string message)
		{
			// everything failed
			if (String.IsNullOrEmpty(message))
			{
				this.RaiseConfigChanged(null);
				return;
			}

			// deserialize the buckets
			var jss = new JavaScriptSerializer();
			var config = jss.Deserialize<ClusterConfig>(message);

			this.RaiseConfigChanged(config);
		}

		private ManualResetEvent mre;
		private MessageStreamListener listener;

		public void Start()
		{
			var reset = this.mre = new ManualResetEvent(false);

			this.listener = this.GetPooledListener();

			reset.WaitOne();
			this.mre = null;

			((IDisposable)reset).Dispose();
		}

		public void Stop()
		{
			this.listener.Unsubscribe(this.HandleMessage);
		}

		private void RaiseConfigChanged(ClusterConfig config)
		{
			var ccc = this.ClusterConfigChanged;

			// we got a new config, notify the pool to reload itself
			if (ccc != null)
				ccc(config);

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
