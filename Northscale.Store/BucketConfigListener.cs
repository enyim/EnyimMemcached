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
		private int? lastHash;
		private ManualResetEvent mre;
		private MessageStreamListener listener;

		public BucketConfigListener(Uri[] poolUrls, string bucketName, NetworkCredential credential)
		{
			this.poolUrls = poolUrls;
			this.bucketName = bucketName ?? "default";

			this.credential = credential;

			this.Timeout = 10000;
			this.DeadTimeout = 10000;
		}

		/// <summary>
		/// Connection timeout in milliseconds for connecting the pool.
		/// </summary>
		public int Timeout { get; set; }

		/// <summary>
		/// Time to wait in milliseconds to reconnect to the pool when all nodes are down.
		/// </summary>
		public int DeadTimeout { get; set; }

		#region listener cache
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
					try
					{ listener.Stop(); }
					catch { }

					listenerRefs.Remove(listener);
					listeners.Remove(info.HashKey);
				}
				else
				{
					info.RefCount--;
				}
			}
		}

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

			lock (ListenerSync)
			{
				if (!listeners.TryGetValue(hash, out retval))
				{
					listeners[hash] = retval = new MessageStreamListener(poolUrls, this.ResolveBucketUri);
					listenerRefs[retval] = new ListenerInfo { RefCount = 1, HashKey = hash };

					retval.Timeout = this.Timeout;
					retval.DeadTimeout = this.DeadTimeout;
					retval.Credentials = this.credential;

					retval.Subscribe(this.HandleMessage);

					retval.Start();
				}
				else
				{
					listenerRefs[retval].RefCount++;
					retval.Subscribe(this.HandleMessage);
				}
			}

			return retval;
		}

		#endregion

		private Uri ResolveBucketUri(WebClientWithTimeout client, Uri uri)
		{
			try
			{
				var helper = new ConfigHelper(client);
				var bucket = helper.ResolveBucket(uri, this.bucketName);

				var streamingUri = bucket.streamingUri;

				var node = bucket.nodes.FirstOrDefault();

				// beta 2 hack, will be phased out after b3 is released for a while
				if (node != null && node.version == "1.6.0beta2")
					streamingUri = streamingUri.Replace("/bucketsStreaming/", "/bucketsStreamingConfig/");

				return new Uri(uri, streamingUri);
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

			// check if the config is the same as the previous
			// we cannot compare the messages because they have more information than we deserialize from them
			var configHash = config.GetHashCode();

			if (lastHash != configHash)
			{
				lastHash = configHash;
				this.RaiseConfigChanged(config);
			}
		}

		public void Start()
		{
			var reset = this.mre = new ManualResetEvent(false);

			// subscribe to the config url
			this.listener = this.GetPooledListener();

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
