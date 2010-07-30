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
		private MessageReader currentWorker;

		public MessageStreamListener(Uri[] urls)
		{
			if (urls == null) throw new ArgumentNullException("urls");
			if (urls.Length == 0) throw new ArgumentException("must specify at least 1 url");

			this.urls = urls;
			this.DeadTimeout = 2000;
		}

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

		protected virtual WebClient CreateClient()
		{
			return new WebClientWithTimeout()
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
		public virtual void Start()
		{
			if (this.IsStarted) throw new InvalidOperationException("already started");

			var success = ThreadPool.QueueUserWorkItem(this.Work);

			if (log.IsDebugEnabled) log.Debug("Starting the listener. Queue=" + success);
		}

		/// <summary>
		/// Stops processing
		/// </summary>
		public virtual void Stop()
		{
			if (log.IsDebugEnabled) log.Debug("Stopping the listener.");

			Interlocked.Exchange(ref this.stopCounter, 1);
			this.currentWorker.Stop();

			this.IsStarted = false;

			if (log.IsDebugEnabled) log.Debug("Stopped.");
		}

		/// <summary>
		/// derived classes can use this to redirect the original url into somewhere else. called only once by urls before the MessageStreamListener starts processing it
		/// </summary>
		/// <param name="uri"></param>
		/// <returns></returns>
		protected virtual Uri ResolveUri(Uri uri)
		{
			return uri;
		}

		private void Work(object state)
		{
			if (log.IsDebugEnabled) log.Debug("Started working.");

			Uri[] urls = this.urls;

			using (var client = this.CreateClient())
			{
				var worker = this.currentWorker = new MessageReader(client, this.OnMessageReceived);

				while (this.stopCounter == 0)
				{
					Uri currentUrl = null;

					int urlIndex = 0;

					// false meens that the url was not responding or failed while reading
					Dictionary<Uri, bool> statusPool = this.urls.ToDictionary(u => u, u => true);
					// this holds the resolved urls, key is coming from the 'urls' array
					Dictionary<Uri, Uri> realUrls = this.urls.ToDictionary(u => u, u => (Uri)null);

					while (this.stopCounter == 0)
					{
						if (log.IsDebugEnabled) log.Debug("finding the first (still) working pool.");

						currentUrl = null;
						int i = urlIndex;

						// find the first working url
						#region [ Find the first working url ]
						while (i < urls.Length)
						{
							var nextUrl = urls[i];

							// check if the url is alive
							if (statusPool[nextUrl])
							{
								try
								{
									// resolve the url
									var realUrl = realUrls[nextUrl] ?? this.ResolveUri(nextUrl);

									if (realUrl != null)
									{
										currentUrl = realUrl;

										if (log.IsDebugEnabled) log.Debug("Found pool url " + currentUrl);

										break;
									}
								}
								catch (Exception e)
								{
									log.Error(e);
								}

								// ResolveUri threw an exception or returned null so mark this url as invalid
								statusPool[nextUrl] = false;
								log.Warn("Could not resolve url " + nextUrl + "; trying the next in the list");
							}

							i++;
						}
						#endregion

						// the break here will go into the outer while, and reinitialize the lookup table and the indexer
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
							log.Error("POOLFAIL", e);

							if (e is IOException || e is System.Net.WebException)
							{
								// current worker failed, most probably the pool it was connected to went down
								if (currentUrl != null)
									statusPool[currentUrl] = false;

								if (log.IsWarnEnabled) log.Warn("Current pool " + currentUrl + " has failed.");
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

						// TODO maybe this should be a separate event
						this.OnMessageReceived(null);

						DateTime now = DateTime.UtcNow;

						var waitUntil = this.DeadTimeout;
						while (this.stopCounter == 0
								&& (DateTime.UtcNow - now).TotalMilliseconds < waitUntil)
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
