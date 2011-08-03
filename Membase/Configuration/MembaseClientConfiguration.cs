using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using Enyim.Caching.Configuration;
using Enyim.Caching.Memcached;
using Enyim.Reflection;

namespace Membase.Configuration
{
	/// <summary>
	/// Configuration class
	/// </summary>
	public class MembaseClientConfiguration : IMembaseClientConfiguration
	{
		private Type nodeLocator;
		private ITranscoder transcoder;
		private IMemcachedKeyTransformer keyTransformer;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:MemcachedClientConfiguration"/> class.
		/// </summary>
		public MembaseClientConfiguration()
		{
			this.Urls = new List<Uri>();

			this.SocketPool = new SocketPoolConfiguration();
		}

		/// <summary>
		/// Gets or sets the name of the bucket to be used. Can be overriden at the pool's constructor, and if not specified the "default" bucket will be used.
		/// </summary>
		public string Bucket { get; set; }

		/// <summary>
		/// Gets or sets the pasword used to connect to the bucket.
		/// </summary>
		/// <remarks> If null, the bucket name will be used. Set to String.Empty to use an empty password.</remarks>
		public string BucketPassword { get; set; }

		/// <summary>
		/// Gets a list of <see cref="T:IPEndPoint"/> each representing a Memcached server in the pool.
		/// </summary>
		public IList<Uri> Urls { get; private set; }

		[Obsolete("Please use the bucket name&password for specifying credentials. This property has no use now, and will be completely removed in the next version.", true)]
		public NetworkCredential Credentials { get; set; }

		/// <summary>
		/// Gets the configuration of the socket pool.
		/// </summary>
		public ISocketPoolConfiguration SocketPool { get; private set; }

		/// <summary>
		/// Gets or sets the <see cref="T:Enyim.Caching.Memcached.IMemcachedKeyTransformer"/> which will be used to convert item keys for Memcached.
		/// </summary>
		public IMemcachedKeyTransformer KeyTransformer
		{
			get { return this.keyTransformer ?? (this.keyTransformer = new DefaultKeyTransformer()); }
			set { this.keyTransformer = value; }
		}

		/// <summary>
		/// Gets or sets the Type of the <see cref="T:Enyim.Caching.Memcached.IMemcachedNodeLocator"/> which will be used to assign items to Memcached nodes.
		/// </summary>
		/// <remarks>If both <see cref="M:NodeLocator"/> and  <see cref="M:NodeLocatorFactory"/> are assigned then the latter takes precedence.</remarks>
		public Type NodeLocator
		{
			get { return this.nodeLocator; }
			set
			{
				ConfigurationHelper.CheckForInterface(value, typeof(IMemcachedNodeLocator));
				this.nodeLocator = value;
			}
		}

		/// <summary>
		/// Gets or sets the NodeLocatorFactory instance which will be used to create a new IMemcachedNodeLocator instances.
		/// </summary>
		/// <remarks>If both <see cref="M:NodeLocator"/> and  <see cref="M:NodeLocatorFactory"/> are assigned then the latter takes precedence.</remarks>
		public IProviderFactory<IMemcachedNodeLocator> NodeLocatorFactory { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="T:Enyim.Caching.Memcached.ITranscoder"/> which will be used serialzie or deserialize items.
		/// </summary>
		public ITranscoder Transcoder
		{
			get { return this.transcoder ?? (this.transcoder = new DefaultTranscoder()); }
			set { this.transcoder = value; }
		}

		/// <summary>
		/// Gets or sets the <see cref="T:Enyim.Caching.Memcached.IPerformanceMonitor"/> instance which will be used monitor the performance of the client.
		/// </summary>
		public IMembasePerformanceMonitorFactory PerformanceMonitorFactory { get; set; }

		public int RetryCount { get; set; }
		public TimeSpan RetryTimeout { get; set; }

		#region [ interface                     ]

		IList<Uri> IMembaseClientConfiguration.Urls
		{
			get { return this.Urls; }
		}

		ISocketPoolConfiguration IMembaseClientConfiguration.SocketPool
		{
			get { return this.SocketPool; }
		}

		IMemcachedKeyTransformer IMembaseClientConfiguration.CreateKeyTransformer()
		{
			return this.KeyTransformer;
		}

		IMemcachedNodeLocator IMembaseClientConfiguration.CreateNodeLocator()
		{
			var f = this.NodeLocatorFactory;
			if (f != null) return f.Create();

			return this.NodeLocator == null
					? new KetamaNodeLocator()
					: (IMemcachedNodeLocator)FastActivator.Create(this.NodeLocator);
		}

		ITranscoder IMembaseClientConfiguration.CreateTranscoder()
		{
			return this.Transcoder;
		}

		string IMembaseClientConfiguration.Bucket
		{
			get { return this.Bucket; }
		}

		int IMembaseClientConfiguration.RetryCount
		{
			get { return this.RetryCount; }
		}

		TimeSpan IMembaseClientConfiguration.RetryTimeout
		{
			get { return this.RetryTimeout; }
		}

		string IMembaseClientConfiguration.BucketPassword
		{
			get { return this.BucketPassword; }
		}

		IPerformanceMonitor IMembaseClientConfiguration.CreatePerformanceMonitor()
		{
			return this.PerformanceMonitorFactory == null
					? null
					: this.PerformanceMonitorFactory.Create(this.Bucket);
		}

		#endregion
	}

	internal class ReadOnlyConfig : IMembaseClientConfiguration
	{
		private string bucket;
		private string bucketPassword;
		private Uri[] urls;
		private TimeSpan retryTimeout;
		private int retryCount;
		private ISocketPoolConfiguration spc;

		private IMembaseClientConfiguration original;

		public ReadOnlyConfig(IMembaseClientConfiguration original)
		{
			this.bucket = original.Bucket;
			this.bucketPassword = original.BucketPassword;
			this.urls = original.Urls.ToArray();

			this.retryCount = original.RetryCount;
			this.retryTimeout = original.RetryTimeout;

			this.spc = new SPC(original.SocketPool);

			this.original = original;
		}

		public void OverrideBucket(string bucketName, string bucketPassword)
		{
			this.bucket = bucketName;
			this.bucketPassword = bucketPassword;
		}

		string IMembaseClientConfiguration.Bucket
		{
			get { return this.bucket; }
		}

		string IMembaseClientConfiguration.BucketPassword
		{
			get { return this.bucketPassword; }
		}

		IList<Uri> IMembaseClientConfiguration.Urls
		{
			get { return this.urls; }
		}

		ISocketPoolConfiguration IMembaseClientConfiguration.SocketPool
		{
			get { return this.spc; }
		}

		IMemcachedKeyTransformer IMembaseClientConfiguration.CreateKeyTransformer()
		{
			return this.original.CreateKeyTransformer();
		}

		IMemcachedNodeLocator IMembaseClientConfiguration.CreateNodeLocator()
		{
			return this.original.CreateNodeLocator();
		}

		ITranscoder IMembaseClientConfiguration.CreateTranscoder()
		{
			return this.original.CreateTranscoder();
		}

		IPerformanceMonitor IMembaseClientConfiguration.CreatePerformanceMonitor()
		{
			return this.original.CreatePerformanceMonitor();
		}

		TimeSpan IMembaseClientConfiguration.RetryTimeout
		{
			get { return this.retryTimeout; }
		}

		int IMembaseClientConfiguration.RetryCount
		{
			get { return this.retryCount; }
		}

		private class SPC : ISocketPoolConfiguration
		{
			private TimeSpan connectionTimeout;
			private TimeSpan deadTimeout;
			private int maxPoolSize;
			private int minPoolSize;
			private TimeSpan queueTimeout;
			private TimeSpan receiveTimeout;
			private INodeFailurePolicyFactory fpf;

			public SPC(ISocketPoolConfiguration original)
			{
				this.connectionTimeout = original.ConnectionTimeout;
				this.deadTimeout = original.DeadTimeout;
				this.maxPoolSize = original.MaxPoolSize;
				this.minPoolSize = original.MinPoolSize;
				this.queueTimeout = original.QueueTimeout;
				this.receiveTimeout = original.ReceiveTimeout;
				this.fpf = original.FailurePolicyFactory;
			}

			int ISocketPoolConfiguration.MinPoolSize { get { return this.minPoolSize; } set { } }
			int ISocketPoolConfiguration.MaxPoolSize { get { return this.maxPoolSize; } set { } }
			TimeSpan ISocketPoolConfiguration.ConnectionTimeout { get { return this.connectionTimeout; } set { } }
			TimeSpan ISocketPoolConfiguration.QueueTimeout { get { return this.queueTimeout; } set { } }
			TimeSpan ISocketPoolConfiguration.ReceiveTimeout { get { return this.receiveTimeout; } set { } }
			TimeSpan ISocketPoolConfiguration.DeadTimeout { get { return this.deadTimeout; } set { } }
			INodeFailurePolicyFactory ISocketPoolConfiguration.FailurePolicyFactory { get { return this.fpf; } set { } }
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
