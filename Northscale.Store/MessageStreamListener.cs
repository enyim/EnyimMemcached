using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace NorthScale.Store
{
	/// <summary>
	/// Listens on a streamingUri and processes the messages
	/// </summary>
	internal class MessageStreamListener
	{
		private static log4net.ILog log = log4net.LogManager.GetLogger(typeof(MessageStreamListener));

		private Uri[] urls;
		private int stopCounter = 0;

		// false means that the url was not responding or failed while reading
		private Dictionary<Uri, bool> statusPool;
		// this holds the resolved urls, key is coming from the 'urls' array
		private Dictionary<Uri, Uri> realUrls;

		private int urlIndex = 0;
		private WebClientWithTimeout client;
		private WebRequest request;
		private WebResponse response;

		private bool hasMessage;
		private string lastMessage;
		private Func<WebClientWithTimeout, Uri, Uri> uriConverter;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="urls"></param>
		/// <param name="converter">you use this to redirect the original url into somewhere else. called only once by urls before the MessageStreamListener starts processing it</param>
		public MessageStreamListener(Uri[] urls, Func<WebClientWithTimeout, Uri, Uri> converter)
		{
			if (urls == null) throw new ArgumentNullException("urls");
			if (urls.Length == 0) throw new ArgumentException("must specify at least 1 url");

			this.urls = urls;
			this.DeadTimeout = 2000;
			this.uriConverter = converter;

			// this holds the resolved urls, key is coming from the 'urls' array
			this.realUrls = this.urls.ToDictionary(u => u, u => (Uri)null);
		}

		protected event Action<string> MessageReceived;
		protected bool IsStarted { get; private set; }

		/// <summary>
		/// The credentials used to connect to the urls.
		/// </summary>
		public ICredentials Credentials { get; set; }

		/// <summary>
		/// Connection timeout in milliseconds for connecting the urls.
		/// </summary>
		public int Timeout { get; set; }

		/// <summary>
		/// The time in milliseconds the listener should wait when retrying after the whole server list goes down.
		/// </summary>
		public int DeadTimeout { get; set; }

		protected WebClientWithTimeout CreateClient()
		{
			return new WebClientWithTimeout
			{
				Credentials = this.Credentials,
				// make it infinite so it will not stop abort the socket thinking that the server have died
				ReadWriteTimeout = System.Threading.Timeout.Infinite,
				// this is just the connect timeout
				Timeout = this.Timeout
			};
		}

		/// <summary>
		/// Starts processing the streaming URI
		/// </summary>
		public void Start()
		{
			if (this.IsStarted) throw new InvalidOperationException("already started");

			this.client = this.CreateClient();
			var success = ThreadPool.QueueUserWorkItem(this.Worker);

			if (log.IsDebugEnabled) log.Debug("Starting the listener. Queue=" + success);
		}

		/// <summary>
		/// Stops processing
		/// </summary>
		public void Stop()
		{
			if (log.IsDebugEnabled) log.Debug("Stopping the listener.");

			Interlocked.Exchange(ref this.stopCounter, 1);
			this.CleanupRequests();

			this.IsStarted = false;

			if (log.IsDebugEnabled) log.Debug("Stopped.");
		}

		public void Subscribe(Action<string> callback)
		{
			if (this.hasMessage)
				callback(this.lastMessage);

			this.MessageReceived += callback;
		}

		public void Unsubscribe(Action<string> callback)
		{
			this.MessageReceived -= callback;
		}

		private void Worker(object state)
		{
			if (log.IsDebugEnabled) log.Debug("Started working.");

			while (this.stopCounter == 0)
			{
				// false means that the url was not responding or failed while reading
				this.statusPool = this.urls.ToDictionary(u => u, u => true);
				this.urlIndex = 0;

				// this will quit when all nodes go down or we're stopped externally
				this.ProcessPool();

				// pool fail
				if (this.stopCounter == 0)
				{
					if (log.IsWarnEnabled) log.Warn("All nodes are dead, sleeping for a while.");

					this.Trigger(null);

					DateTime now = DateTime.UtcNow;

					var waitUntil = this.DeadTimeout;
					while (this.stopCounter == 0
							&& (DateTime.UtcNow - now).TotalMilliseconds < waitUntil)
					{
						Thread.Sleep(100);
					}
				}
			}
		}

		private void ProcessPool()
		{
			while (this.stopCounter == 0)
			{
				if (log.IsDebugEnabled) log.Debug("Looking for the first working node.");

				// key is the original url (used for the status dictionary)
				// value is the resolved url (used for receiving messages)
				var current = GetNextPoolUri();

				// the break here will go into the outer while, and reinitialize the lookup table and the indexer
				if (current.Key == null)
				{
					if (log.IsWarnEnabled) log.Debug("Could not found a working node.");

					return;
				}

				try
				{
					if (log.IsDebugEnabled) log.Debug("Start receiving messages.");

					// start working on the current url
					// if it fails in the meanwhile, we'll get another url
					this.ReadMessages(current.Value);

					if (this.stopCounter > 0)
					{
						if (log.IsDebugEnabled) log.Debug("Processing is aborted.");

						return;
					}
					else if (log.IsWarnEnabled) log.Warn("Streaming uri stopped streaming");
				}
				catch (Exception e)
				{
					if (e is IOException || e is System.Net.WebException)
					{
						// current node url failed, most probably the server was removed from the pool (or just failed)
						if (current.Key != null)
							statusPool[current.Key] = false;

						if (log.IsWarnEnabled) log.Warn("Current node '" + current.Value + "' has failed.");
					}
					else
					{
						if (log.IsErrorEnabled) log.Error("Unexpected pool failure.", e);
						throw;
					}
				}
			}
		}

		private KeyValuePair<Uri, Uri> GetNextPoolUri()
		{
			var i = this.urlIndex;

			while (i < this.urls.Length)
			{
				var key = this.urls[i];

				// check if the url is alive
				if (this.statusPool[key])
				{
					try
					{
						// resolve the url
						var resolved = realUrls[key] ?? this.uriConverter(this.client, key);

						if (resolved != null)
						{
							if (log.IsDebugEnabled) log.Debug("Resolved pool url " + key + " to " + resolved);

							return new KeyValuePair<Uri, Uri>(key, resolved);
						}
					}
					catch (Exception e)
					{
						log.Error(e);
					}

					// ResolveUri threw an exception or returned null so mark this url as invalid
					statusPool[key] = false;
					log.Warn("Could not resolve url " + key + "; trying the next in the list");
				}

				i++;
			}

			return new KeyValuePair<Uri, Uri>();
		}

		private void CleanupRequests()
		{
			if (this.request != null)
			{
				try { this.request.Abort(); }
				catch { }
			}

			if (this.response != null)
			{
				try { ((IDisposable)this.response).Dispose(); }
				catch { }
			}

			this.request = null;
			this.response = null;
		}

		private void Trigger(string message)
		{
			var mr = this.MessageReceived;
			if (mr != null)
				mr(message);

			this.lastMessage = message;
			this.hasMessage = true;
		}

		private void ReadMessages(Uri uri)
		{
			if (this.stopCounter > 0) return;

			this.CleanupRequests();

			this.request = this.client.GetWebRequest(uri, uri.GetHashCode().ToString());
			this.response = this.request.GetResponse();

			// the url is supposed to send data indefinitely
			// the only way out of here is either calling Stop() or failing by an exception
			// somehow stream.Dispose() hangs, so we skipping it for a while
			var stream = this.response.GetResponseStream();
			var reader = new StreamReader(stream, Encoding.UTF8, false);

			string line;
			var emptyCounter = 0;
			var messageBuilder = new StringBuilder();

			while ((line = reader.ReadLine()) != null)
			{
				if (this.stopCounter > 0)
					return;

				if (line.Length == 0)
				{
					emptyCounter++;

					// messages are separated by 3 empty lines
					if (emptyCounter == 3)
					{
						this.Trigger(messageBuilder.ToString());
						messageBuilder.Length = 0;
						emptyCounter = 0;
					}
				}
				else
				{
					emptyCounter = 0;
					messageBuilder.Append(line);
				}
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
