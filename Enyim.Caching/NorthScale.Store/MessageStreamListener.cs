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
		private Thread thread;
		private int stopCounter = 0;
		private MessageReader currentWorker;

		public MessageStreamListener(Uri[] urls)
		{
			if (urls == null) throw new ArgumentNullException("urls");
			if (urls.Length == 0) throw new ArgumentException("must specify at least 1 url");

			this.urls = urls;
		}

		public ICredentials Credentials { get; set; }

		protected virtual WebClient CreateClient()
		{
			var client = new WebClient();

			client.Credentials = this.Credentials;
			client.Headers[HttpRequestHeader.CacheControl] = "no-cache";
			client.Headers[HttpRequestHeader.Accept] = "application/com.northscale.store+json";
			client.Headers[HttpRequestHeader.UserAgent] = "enyim.com memcached client";

			client.Encoding = Encoding.UTF8;

			return client;
		}

		/// <summary>
		/// Starts processing the streaming URI
		/// </summary>
		public void Start(int timeout)
		{
			if (this.thread != null) throw new InvalidOperationException("already started");

			var t = new Thread(this.Work);
			t.Priority = ThreadPriority.BelowNormal;

			this.thread = t;
			t.Name = "MessageStreamListener";

			if (log.IsDebugEnabled) log.Debug("Starting the listener.");

			t.Start(timeout);
		}

		/// <summary>
		/// Stops processing
		/// </summary>
		public void Stop()
		{
			if (log.IsDebugEnabled) log.Debug("Stopping the listener.");

			Interlocked.Exchange(ref this.stopCounter, 1);
			this.currentWorker.Stop();

			if (this.thread.ThreadState == ThreadState.Running)
			{
				if (log.IsDebugEnabled) log.Debug("Thread is still running, doing a Join().");

				this.thread.Join(500);
				if (this.thread.ThreadState == ThreadState.Running)
				{
					if (log.IsDebugEnabled) log.Debug("Thread is still running, aborting.");

					this.thread.Abort();
				}
			}

			this.thread = null;

			if (log.IsDebugEnabled) log.Debug("Stopped.");
		}

		private void Work(object state)
		{
			if (log.IsDebugEnabled) log.Debug("Started working.");

			if (state != null)
			{
				int sleep = (int)state;
				if (sleep > 0)
				{
					if (log.IsDebugEnabled) log.Debug("Sleeping for " + sleep + " msec");

					Thread.Sleep(sleep);
				}
			}

			Uri[] urls = this.urls;
			using (var client = ConfigHelper.CreateClient(this.Credentials))
			{
				var worker = this.currentWorker = new MessageReader(client, this.OnMessageReceived);

				while (this.stopCounter == 0)
				{
					Uri currentUrl = null;

					int urlIndex = 0;
					// false meens that the url was not responding or failed while reading
					Dictionary<Uri, bool> urlPool = this.urls.ToDictionary(u => u, u => true);

					while (this.stopCounter == 0)
					{
						if (log.IsDebugEnabled) log.Debug("finding the first (still) working pool.");

						currentUrl = null;
						int i = urlIndex;

						// find the first working url
						while (i < urls.Length)
						{
							var tmp = urls[i];
							if (urlPool[tmp])
							{
								currentUrl = tmp;

								if (log.IsDebugEnabled) log.Debug("Found pool url " + tmp);

								break;
							}

							i++;
						}

						// the break here will bo into the outer while, and reinitialize the lookup table and the indexer
						if (currentUrl == null)
						{
							if (log.IsWarnEnabled) log.Debug("Could not found a working pool url.");
							break;
						}

						// store the current index
						urlIndex = i;

						try
						{
							if (log.IsDebugEnabled) log.Debug("Start receiving messages.");

							// start working on the current url
							// if it fails in the meanwhile, we'll get another url
							worker.Start(currentUrl);

							if (log.IsDebugEnabled) log.Debug("MessageReader has exited");

							if (this.stopCounter > 0)
							{
								if (log.IsDebugEnabled) log.Debug("Processing is aborted.");

								return;
							}
						}
						catch (Exception e)
						{
							if (e is IOException || e is System.Net.WebException)
							{
								// current worker failed, most probably the pool it was connected to went down
								if (currentUrl != null && urlPool != null)
									urlPool[currentUrl] = false;

								if (log.IsWarnEnabled) log.Warn("Current pool " + currentUrl + " has failed.");

								this.OnConnectionAborted();
							}
							else
							{
								if (log.IsErrorEnabled) log.Error("Fatal error.", e);
								throw;
							}
						}
					}

					// should we exit, or sleep because no available were found?
					if (this.stopCounter == 0)
					{
						if (log.IsWarnEnabled) log.Warn("All pools are dead, sleeping a while.");

						DateTime now = DateTime.UtcNow;
						while (stopCounter == 0 && (DateTime.UtcNow - now).TotalSeconds < 5)
						{
							Thread.Sleep(200);
						}
					}
				}
			}
		}

		protected virtual void OnMessageReceived(string message)
		{
			//if (log.IsDebugEnabled) log.Debug("Received message " + message);
		}

		protected virtual void OnConnectionAborted()
		{
			//if (log.IsDebugEnabled) log.Debug("Aborted");
		}

		#region [ MessageReader                ]
		private class MessageReader
		{
			private int stopCounter;
			private WebClient client;
			private Action<string> callback;

			public MessageReader(WebClient client, Action<string> callback)
			{
				this.client = client;
				this.callback = callback;
			}

			public void Start(Uri uri)
			{
				// the url is supposed to send data indefinitely
				// but if it finishes normally somehow, this 'while' will  keep things working
				// the only way out of here is either calling Stop() failing by an exception
				while (true)
				{
					// somehow stream.Dispose() hangs, so we skipping it for a while
					var stream = this.client.OpenRead(uri);
					var reader = new StreamReader(stream, Encoding.UTF8, false);

					if (this.stopCounter > 0)
						return;

					string line;
					int emptyCounter = 0;
					StringBuilder messageBuilder = new StringBuilder();

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
								this.callback(messageBuilder.ToString());
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

			public void Stop()
			{
				this.client.CancelAsync();
				Interlocked.Exchange(ref this.stopCounter, 1);
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
