//#define DEBUG_RETRY
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace Membase
{
	/// <summary>
	/// Listens on a streamingUri and processes the messages
	/// </summary>
	internal class MessageStreamListener : IDisposable
	{
		private static readonly Enyim.Caching.ILog log = Enyim.Caching.LogManager.GetLogger(typeof(MessageStreamListener));

		private Uri[] urls;
		private ManualResetEvent stopEvent;
		private ManualResetEvent sleepEvent;
		private WaitHandle[] sleepHandles;

		// false means that the url was not responding or failed while reading
		private Dictionary<Uri, bool> statusPool;
		// this holds the resolved urls, key is coming from the 'urls' array
		private Dictionary<Uri, Uri> realUrls;

		private int urlIndex = 0;
		private WebClientWithTimeout requestFactory;

		private WebRequest request;
		private WebResponse response;
		private Heartbeat heartbeat;

		private bool hasMessage;
		private string lastMessage;
		private Func<WebClientWithTimeout, Uri, Uri> uriConverter;

		/// <summary>
		/// Creates a new instance of MessageStreamListener
		/// </summary>
		/// <param name="urls"></param>
		/// <param name="converter">You use this to redirect the original url into somewhere else. Called only once for each url before the MessageStreamListener starts processing it.</param>
		public MessageStreamListener(Uri[] urls, Func<WebClientWithTimeout, Uri, Uri> converter)
		{
			if (urls == null) throw new ArgumentNullException("urls");
			if (urls.Length == 0) throw new ArgumentException("must specify at least 1 url");

			this.urls = urls;
			this.DeadTimeout = 2000;
			this.uriConverter = converter;

			this.stopEvent = new ManualResetEvent(false);
			this.sleepEvent = new ManualResetEvent(false);
			this.sleepHandles = new[] { stopEvent, sleepEvent };

			// this holds the resolved urls, key is coming from the 'urls' array
			this.realUrls = this.urls.Distinct().ToDictionary(u => u, u => (Uri)null);

			this.RetryCount = 0;
			this.RetryTimeout = new TimeSpan(0, 0, 0, 0, 500);

			// domain unloads are not guaranteed to call the finalizers
			// and when users do not abide the IDIsposabel contract
			// we end up a job in the thread pool which never quits
			// and prevents the unloading of the app domain
			// this is not big deal in normal applications because the
			// process exit aborts everything, but it's a huge issue in
			// asp.net because the app domain will not be unloaded
			// but still, the best way to deal with this is issue to dispose the client
			AppDomain.CurrentDomain.DomainUnload += CurrentDomain_DomainUnload;
		}

		void CurrentDomain_DomainUnload(object sender, EventArgs e)
		{
			this.Dispose();
		}

		~MessageStreamListener()
		{
			try { this.Dispose(); }
			catch { }
		}

		protected event Action<string> MessageReceived;
		protected bool IsStarted { get; private set; }
		public int RetryCount { get; set; }
		public TimeSpan RetryTimeout { get; set; }

		/// <summary>
		/// The credentials used to connect to the urls.
		/// </summary>
		public NetworkCredential Credentials { get; set; }

		/// <summary>
		/// Connection timeout in milliseconds for connecting the urls.
		/// </summary>
		public int ConnectionTimeout { get; set; }

		/// <summary>
		/// The time in milliseconds the listener should wait when retrying after the whole server list goes down.
		/// </summary>
		public int DeadTimeout { get; set; }

		protected WebClientWithTimeout CreateRequestFactory()
		{
			return new WebClientWithTimeout
			{
				Credentials = this.Credentials,
				// make it infinite so it will not stop abort the socket thinking that the server have died
				ReadWriteTimeout = System.Threading.Timeout.Infinite,
				// this is just the connect timeout
				Timeout = this.ConnectionTimeout,
				PreAuthenticate = true
			};
		}

		/// <summary>
		/// Starts processing the streaming URI
		/// </summary>
		public void Start()
		{
			if (this.IsStarted) throw new InvalidOperationException("already started");

			var success = ThreadPool.QueueUserWorkItem(this.Worker);

			if (log.IsDebugEnabled) log.Debug("Starting the listener. Queue=" + success);
		}

		/// <summary>
		/// Stops processing
		/// </summary>
		public void Stop()
		{
			if (log.IsDebugEnabled) log.Debug("Stopping the listener.");

			this.stopEvent.Set();
			this.AbortRequests();

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

			while (!this.stopEvent.WaitOne(0))
			{
				if (this.requestFactory == null)
					this.requestFactory = this.CreateRequestFactory();

				// false means that the url was not responding or failed while reading
				this.statusPool = this.urls.ToDictionary(u => u, u => true);
				this.urlIndex = 0;

				// this will quit when all nodes go down or we're stopped externally
				this.ProcessPool();

				// pool fail
				if (!this.stopEvent.WaitOne(0))
				{
					if (log.IsWarnEnabled) log.Warn("All nodes are dead, sleeping for a while.");

					this.Trigger(null);

					this.SleepUntil(this.DeadTimeout);

					// recreate the client after failure
					this.AbortRequests();
					if (this.requestFactory != null)
					{
						this.requestFactory.Dispose();
						this.requestFactory = null;
					}
				}
			}
		}

		/// <summary>
		/// Sleeps until the time elapses. Returns false if the sleep was aborted.
		/// </summary>
		/// <param name="milliseconds"></param>
		/// <returns></returns>
		private bool SleepUntil(int milliseconds)
		{
			return (WaitHandle.WaitAny(sleepHandles, milliseconds) != 0);
		}

		private int currentRetryCount;

		private void ProcessPool()
		{
			while (!this.stopEvent.WaitOne(0))
			{
				if (log.IsDebugEnabled) log.Debug("Looking for the first working node.");

				// key is the original url (used for the status dictionary)
				// value is the resolved url (used for receiving messages)
				var current = GetNextPoolUri();

				if (current.Key == null)
				{
					if (log.IsWarnEnabled) log.Warn("Could not found a working node.");

					return;
				}

				try
				{
					if (log.IsDebugEnabled) log.Debug("Start receiving messages.");
					this.currentRetryCount = 0;

					while (!this.stopEvent.WaitOne(0))
					{
						try
						{
							// start working on the current url
							// if it fails in the meanwhile, we'll get another url
							this.ReadMessages(current.Key, current.Value);

							// we can only get here properly if the listener is stopped, so just quit the whole loop
							if (log.IsDebugEnabled) log.Debug("Processing is aborted.");
							return;
						}
						catch (Exception x)
						{
							if (log.IsDebugEnabled) log.Debug("ReadMessage failed with exception:", x);

							if (this.currentRetryCount == this.RetryCount)
							{
								if (log.IsDebugEnabled) log.Debug("Reached the retry limit, rethrowing.", x);
								throw;
							}
						}

						if (log.IsDebugEnabled) log.DebugFormat("Counter is {0}, sleeping for {1} then retrying.", this.currentRetryCount, this.RetryTimeout);

						SleepUntil((int)this.RetryTimeout.TotalMilliseconds);
						this.currentRetryCount++;
					}
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
						var resolved = realUrls[key] ?? this.uriConverter(this.requestFactory, key);

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

		private void AbortRequests()
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

			if (this.heartbeat != null)
			{
				try { ((IDisposable)this.heartbeat).Dispose(); }
				catch { }
			}

			this.request = null;
			this.response = null;
			this.heartbeat = null;
		}

		private void Trigger(string message)
		{
			var mr = this.MessageReceived;
			if (mr != null)
				mr(message);

			if (log.IsDebugEnabled)
				log.Debug("Processing message: " + message);

			this.lastMessage = message;
			this.hasMessage = true;
		}

		private void ReadMessages(Uri heartBeatUrl, Uri configUrl)
		{
			if (this.stopEvent.WaitOne(0)) return;

#if DEBUG_RETRY
			ThreadPool.QueueUserWorkItem(o =>
			{
				Thread.Sleep(4000);
				CleanupRequests();
			});

			if (this.currentRetryCount > 0) throw new IOException();
#endif
			this.AbortRequests();

			this.request = this.requestFactory.GetWebRequest(configUrl, configUrl.GetHashCode().ToString());
			this.response = this.request.GetResponse();

			// the url is supposed to send data indefinitely
			// the only way out of here is either calling Stop() or failing by an exception
			// (somehow stream.Dispose() hangs, so we'll not use the 'using' construct)
			var stream = this.response.GetResponseStream();
			var reader = new StreamReader(stream, Encoding.UTF8, false);

			// TODO make the 10 seconds configurable (it if makes sense)
			using (this.heartbeat = new Heartbeat(heartBeatUrl, this.ConnectionTimeout, 10 * 1000, this.AbortRequests, this.Credentials))
			{
				string line;
				var emptyCounter = 0;
				var messageBuilder = new StringBuilder();

				while ((line = reader.ReadLine()) != null)
				{
					if (this.stopEvent.WaitOne(0)) return;

					// we're successfully reading the stream, so reset the retry counter
					this.currentRetryCount = 0;

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

			if (log.IsErrorEnabled)
				log.Error("The infinite loop just finished, probably the server closed the connection without errors. (?)");

			throw new IOException("Remote host closed the streaming connection");
		}

		void IDisposable.Dispose()
		{
			this.Dispose();
		}

		protected void Dispose()
		{
			AppDomain.CurrentDomain.DomainUnload -= CurrentDomain_DomainUnload;

			this.AbortRequests();

			if (this.requestFactory != null)
			{
				using (this.requestFactory)
					this.requestFactory.CancelAsync();

				this.requestFactory = null;
			}
		}

		#region [ Heartbeat                    ]

		private class Heartbeat : IDisposable
		{
			private Timer timer;
			private Uri uri;

			private int shouldAbort;
			private int interval;
			private int timeout;

			private WebRequest request;
			private WebResponse response;
			private Action abortAction;
			private NetworkCredential credentials;

			public Heartbeat(Uri uri, int timeout, int interval, Action abortAction, NetworkCredential credentials)
			{
				this.uri = uri;
				this.interval = interval;
				this.timeout = timeout;
				this.abortAction = abortAction;
				this.credentials = credentials;

				this.timer = new Timer(this.Worker, null, interval, Timeout.Infinite);
			}

			void IDisposable.Dispose()
			{
				if (this.shouldAbort > 0) return;

				Interlocked.Exchange(ref this.shouldAbort, 2);

				this.timer.Change(Timeout.Infinite, Timeout.Infinite);

				if (this.request != null)
				{
					try { this.request.Abort(); }
					catch { }

					this.request = null;
				}

				if (this.response != null)
				{
					this.response.Close();
					this.response = null;
				}

				this.timer.Dispose();
				this.timer = null;
			}

			private void Worker(object state)
			{
				if (log.IsDebugEnabled) log.DebugFormat("HB: Pinging current node '{0}' to check if it's still alive.", this.uri);

				if (this.shouldAbort > 0)
				{
					if (log.IsDebugEnabled) log.DebugFormat("HB: Already aborted {0}, returning.", this.uri);

					return;
				}

				var req = WebRequest.Create(this.uri) as HttpWebRequest;

				if (this.credentials != null)
					req.Credentials = this.credentials;

				req.Timeout = this.timeout;
				req.ReadWriteTimeout = this.timeout;
				req.Pipelined = false;
				req.Method = "GET";
				req.KeepAlive = false;

				this.request = req;

				try
				{
					log.DebugFormat("HB: Trying '{0}'", this.uri);

					this.response = request.GetResponse();

					using (var stream = response.GetResponseStream())
					using (var sr = new StreamReader(stream))
					{
						sr.ReadToEnd();

						log.DebugFormat("HB: Node '{0}' is OK", this.uri);
					}

					if (this.shouldAbort == 0)
						this.timer.Change(this.interval, Timeout.Infinite);
				}
				catch (Exception e)
				{
					log.ErrorFormat("HB: Node '{0}' is not available.\n{1}", this.uri, e);

					if (this.shouldAbort == 0)
						this.abortAction();
				}
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
