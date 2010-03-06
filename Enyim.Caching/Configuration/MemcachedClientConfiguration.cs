using System;
using System.Collections.Generic;
using System.Net;
using Enyim.Caching.Memcached;

namespace Enyim.Caching.Configuration
{
	/// <summary>
	/// COnfiguration class
	/// </summary>
	public class MemcachedClientConfiguration : IMemcachedClientConfiguration
	{
		private List<IPEndPoint> servers;
		private ISocketPoolConfiguration socketPool;
		private Type keyTransformer;
		private Type nodeLocator;
		private Type transcoder;
		private MemcachedProtocol protocol;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:MemcachedClientConfiguration"/> class.
		/// </summary>
		public MemcachedClientConfiguration()
		{
			this.servers = new List<IPEndPoint>();
			this.socketPool = new _SocketPoolConfig();

			this.EnablePerformanceCounters = false;
			this.Protocol = MemcachedProtocol.Text;
		}

		/// <summary>
		/// Gets a list of <see cref="T:IPEndPoint"/> each representing a Memcached server in the pool.
		/// </summary>
		public IList<IPEndPoint> Servers
		{
			get { return this.servers; }
		}

		/// <summary>
		/// Gets the configuration of the socket pool.
		/// </summary>
		public ISocketPoolConfiguration SocketPool
		{
			get { return this.socketPool; }
		}

		/// <summary>
		/// Gets or sets the type of the <see cref="T:Enyim.Caching.Memcached.IMemcachedKeyTransformer"/> which will be used to convert item keys for Memcached.
		/// </summary>
		public Type KeyTransformer
		{
			get { return this.keyTransformer; }
			set
			{
				ConfigurationHelper.CheckForInterface(value, typeof(IMemcachedKeyTransformer));

				this.keyTransformer = value;
			}
		}

		/// <summary>
		/// Gets or sets the type of the <see cref="T:Enyim.Caching.Memcached.IMemcachedNodeLocator"/> which will be used to assign items to Memcached nodes.
		/// </summary>
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
		/// Gets or sets the type of the <see cref="T:Enyim.Caching.Memcached.ITranscoder"/> which will be used serialzie or deserialize items.
		/// </summary>
		public Type Transcoder
		{
			get { return this.transcoder; }
			set
			{
				ConfigurationHelper.CheckForInterface(value, typeof(ITranscoder));

				this.transcoder = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether operation statistics are created using Windows Performance Counters.
		/// </summary>
		/// <remarks>This is set to false by default so the application using this library will work even if teh performance counters are not installed.</remarks>
		public bool EnablePerformanceCounters { get; set; }

		/// <summary>
		/// Gets or sets the type of the communication between client and server.
		/// </summary>
		public MemcachedProtocol Protocol
		{
			get { return this.protocol; }
			set { this.protocol = value; }
		}

		#region [ IMemcachedClientConfiguration]

		IList<System.Net.IPEndPoint> IMemcachedClientConfiguration.Servers
		{
			get { return this.Servers; }
		}

		ISocketPoolConfiguration IMemcachedClientConfiguration.SocketPool
		{
			get { return this.SocketPool; }
		}

		Type IMemcachedClientConfiguration.KeyTransformer
		{
			get { return this.KeyTransformer; }
			set { this.KeyTransformer = value; }
		}

		Type IMemcachedClientConfiguration.NodeLocator
		{
			get { return this.NodeLocator; }
			set { this.NodeLocator = value; }
		}

		Type IMemcachedClientConfiguration.Transcoder
		{
			get { return this.Transcoder; }
			set { this.Transcoder = value; }
		}

		bool IMemcachedClientConfiguration.EnablePerformanceCounters
		{
			get { return this.EnablePerformanceCounters; }
			set { this.EnablePerformanceCounters = value; }
		}

		MemcachedProtocol IMemcachedClientConfiguration.Protocol
		{
			get { return this.protocol; }
			set { this.protocol = value; }
		}
		#endregion
		#region [ T:SocketPoolConfig           ]
		private class _SocketPoolConfig : ISocketPoolConfiguration
		{
			private int minPoolSize = 10;
			private int maxPoolSize = 200;
			private TimeSpan connectionTimeout = new TimeSpan(0, 0, 10);
			private TimeSpan receiveTimeout = new TimeSpan(0, 0, 10);
			private TimeSpan deadTimeout = new TimeSpan(0, 2, 0);

			int ISocketPoolConfiguration.MinPoolSize
			{
				get { return this.minPoolSize; }
				set
				{
					if (value > 1000 || value > this.maxPoolSize)
						throw new ArgumentOutOfRangeException("value", "MinPoolSize must be <= MaxPoolSize and must be <= 1000");

					this.minPoolSize = value;
				}
			}

			int ISocketPoolConfiguration.MaxPoolSize
			{
				get { return this.maxPoolSize; }
				set
				{
					if (value > 1000 || value < this.minPoolSize)
						throw new ArgumentOutOfRangeException("value", "MaxPoolSize must be >= MinPoolSize and must be <= 1000");

					this.maxPoolSize = value;
				}
			}

			TimeSpan ISocketPoolConfiguration.ConnectionTimeout
			{
				get { return this.connectionTimeout; }
				set
				{
					if (value < TimeSpan.Zero)
						throw new ArgumentOutOfRangeException("value", "value must be positive");

					this.connectionTimeout = value;
				}
			}

			TimeSpan ISocketPoolConfiguration.ReceiveTimeout
			{
				get { return this.receiveTimeout; }
				set
				{
					if (value < TimeSpan.Zero)
						throw new ArgumentOutOfRangeException("value", "value must be positive");

					this.receiveTimeout = value;
				}
			}

			TimeSpan ISocketPoolConfiguration.DeadTimeout
			{
				get { return this.deadTimeout; }
				set
				{
					if (value < TimeSpan.Zero)
						throw new ArgumentOutOfRangeException("value", "value must be positive");

					this.deadTimeout = value;
				}
			}
		}
		#endregion
	}
}

#region [ License information          ]
/* ************************************************************
 *
 * Copyright (c) Attila Kiskó, enyim.com
 *
 * This source code is subject to terms and conditions of 
 * Microsoft Permissive License (Ms-PL).
 * 
 * A copy of the license can be found in the License.html
 * file at the root of this distribution. If you can not 
 * locate the License, please send an email to a@enyim.com
 * 
 * By using this source code in any fashion, you are 
 * agreeing to be bound by the terms of the Microsoft 
 * Permissive License.
 *
 * You must not remove this notice, or any other, from this
 * software.
 *
 * ************************************************************/
#endregion