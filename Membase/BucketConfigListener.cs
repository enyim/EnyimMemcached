using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;
using System.Threading;
using System.Net;
using Enyim;
using Membase.Configuration;

namespace Membase
{
	internal class BucketConfigListener
	{
		private static readonly Enyim.Caching.ILog log = Enyim.Caching.LogManager.GetLogger(typeof(BucketConfigListener));

		private Uri[] poolUrls;
		private string bucketName;
		private NetworkCredential credential;
		private int? lastHash;
		private ManualResetEvent mre;
		private MessageStreamListener listener;

		public BucketConfigListener(Uri[] poolUrls, string bucketName, string bucketPassword)
		{
			this.poolUrls = poolUrls;
			this.bucketName = String.IsNullOrEmpty(bucketName)
								? "default"
								: bucketName;

			this.credential = bucketName == "default"
								? null
								: new NetworkCredential(bucketName, bucketPassword);

			this.Timeout = 10000;
			this.DeadTimeout = 10000;

			this.RetryCount = 0;
			this.RetryTimeout = new TimeSpan(0, 0, 0, 0, 500);
		}

		/// <summary>
		/// Connection timeout in milliseconds for connecting the pool.
		/// </summary>
		public int Timeout { get; set; }

		public int RetryCount { get; set; }
		public TimeSpan RetryTimeout { get; set; }

		/// <summary>
		/// Time to wait in milliseconds to reconnect to the pool when all nodes are down.
		/// </summary>
		public int DeadTimeout { get; set; }

		/// <summary>
		/// Raised when the pool's configuration changes.
		/// </summary>
		public event Action<ClusterConfig> ClusterConfigChanged;

		/// <summary>
		/// Starts listening for configuration data. This method blocks until the initial configuration is received. (Or until all pool urls fail.)
		/// </summary>
		public void Start()
		{
			var reset = this.mre = new ManualResetEvent(false);

			// subscribe to the config url
			this.listener = this.GetPooledListener();

			// this will be signaled by the config changed event handler
			reset.WaitOne();

			// set to null, then dispose, so RaiseConfigChanged will not 
			// fail at Set when the config changes while we're cleaning up here
			this.mre = null;
			((IDisposable)reset).Dispose();
		}

		public void Stop()
		{
			this.ReleaseListener(this.listener);
			this.listener = null;
		}

		private static readonly JavaScriptConverter[] KnownConverters = { ClusterNode.ConverterInstance };

		private void HandleMessage(string message)
		{
			// everything failed
			if (String.IsNullOrEmpty(message))
			{
				this.lastHash = null;
				this.RaiseConfigChanged(null);
				return;
			}

			// deserialize the buckets
			var jss = new JavaScriptSerializer();
			jss.RegisterConverters(KnownConverters);

			var config = jss.Deserialize<ClusterConfig>(message);

			// check if the config is the same as the previous
			// we cannot compare the messages because they have more information than we deserialize from them
			var configHash = config.GetHashCode();

			if (lastHash != configHash)
			{
				lastHash = configHash;
				this.RaiseConfigChanged(config);
			}
			else if (log.IsDebugEnabled)
				log.Debug("Last message was the same as current, ignoring.");
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

		#region [ message listener pooling     ]
		private static readonly object ListenerSync = new Object();

		// we pool and refcount the listeners here so we can safely dispose them when all clients are destroyed
		private static Dictionary<int, MessageStreamListener> listeners = new Dictionary<int, MessageStreamListener>();
		private static Dictionary<MessageStreamListener, ListenerInfo> listenerRefs = new Dictionary<MessageStreamListener, ListenerInfo>();

		private class ListenerInfo
		{
			public int RefCount;
			public int HashKey;
		}

		/// <summary>
		/// Unsubscibes from a pooled listener, and destrpys it if no additionals subscribers are present.
		/// </summary>
		/// <param name="listener"></param>
		private void ReleaseListener(MessageStreamListener listener)
		{
			lock (ListenerSync)
			{
				listener.Unsubscribe(this.HandleMessage);

				var info = listenerRefs[listener];
				if (info.RefCount == 1)
				{
					listenerRefs.Remove(listener);
					listeners.Remove(info.HashKey);

					try { using (listener) listener.Stop(); }
					catch { }
				}
				else
				{
					info.RefCount--;
				}
			}
		}

		/// <summary>
		/// Returns a MessageStreamListener instance based on this instance's configuratino (timeout, bucket name etc.)
		/// 
		/// When multiple listeners are requested with the exact same parameters (usually when multiple clients are instantiated from the same configuration),
		/// the same listener will be returned each time.
		/// </summary>
		/// <returns></returns>
		private MessageStreamListener GetPooledListener()
		{
			// create a unique key based on the parameters
			// to find out if we already have a listener attached to this pool
			var hcc = new HashCodeCombiner();

			hcc.Add(this.Timeout);
			hcc.Add(this.DeadTimeout);
			hcc.Add(this.RetryCount);
			hcc.Add(this.RetryTimeout.GetHashCode());
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

			lock (ListenerSync)
				if (listeners.TryGetValue(hash, out retval))
				{
					listenerRefs[retval].RefCount++;
					retval.Subscribe(this.HandleMessage);
				}
				else
				{
					var name = this.bucketName;

					// create a new listener for the pool urls
					retval = new MessageStreamListener(poolUrls, (client, root) => ResolveBucketUri(client, root, name));

					retval.Timeout = this.Timeout;
					retval.DeadTimeout = this.DeadTimeout;
					retval.Credentials = this.credential;
					retval.RetryCount = this.RetryCount;
					retval.RetryTimeout = this.RetryTimeout;

					retval.Subscribe(this.HandleMessage);

					listeners[hash] = retval;
					listenerRefs[retval] = new ListenerInfo { RefCount = 1, HashKey = hash };

					retval.Start();
				}

			return retval;
		}

		private static Uri ResolveBucketUri(WebClientWithTimeout client, Uri root, string bucketName)
		{
			try
			{
				var bucket = ConfigHelper.ResolveBucket(client, root, bucketName);
				if (bucket == null)
					return null;

				if (String.IsNullOrEmpty(bucket.streamingUri))
				{
					log.ErrorFormat("Url {0} for bucket {1} returned a config with no streamingUri", root, bucketName);
					return null;
				}

				return new Uri(root, bucket.streamingUri);
			}
			catch (Exception e)
			{
				log.Error("Error resolving streaming uri: " + root, e);

				return null;
			}
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
