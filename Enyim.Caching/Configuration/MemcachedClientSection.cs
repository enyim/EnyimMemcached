using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Net;
using System.Web.Configuration;
using Enyim.Caching.Memcached;

namespace Enyim.Caching.Configuration
{
	/// <summary>
	/// Configures the <see cref="T:MemcachedClient"/>. This class cannot be inherited.
	/// </summary>
	public sealed class MemcachedClientSection : ConfigurationSection, IMemcachedClientConfiguration
	{
		/// <summary>
		/// Returns a collection of Memcached servers which can be used by the client.
		/// </summary>
		[ConfigurationProperty("servers", IsRequired = true)]
		public EndPointElementCollection Servers
		{
			get { return (EndPointElementCollection)base["servers"]; }
		}

		/// <summary>
		/// Gets or sets the configuration of the socket pool.
		/// </summary>
		[ConfigurationProperty("socketPool", IsRequired = false)]
		public SocketPoolElement SocketPool
		{
			get { return (SocketPoolElement)base["socketPool"]; }
			set { base["socketPool"] = value; }
		}

		/// <summary>
		/// Gets or sets the configuration of the authenticator.
		/// </summary>
		[ConfigurationProperty("authentication", IsRequired = false)]
		public AuthenticationElement Authentication
		{
			get { return (AuthenticationElement)base["authentication"]; }
			set { base["authentication"] = value; }
		}

		/// <summary>
		/// Gets or sets the type of the <see cref="T:Enyim.Caching.Memcached.IMemcachedKeyTransformer"/> which will be used to convert item keys for Memcached.
		/// </summary>
		[ConfigurationProperty("keyTransformer", IsRequired = false), TypeConverter(typeof(TypeNameConverter)), InterfaceValidator(typeof(Enyim.Caching.Memcached.IMemcachedKeyTransformer))]
		public Type KeyTransformer
		{
			get { return (Type)base["keyTransformer"]; }
			set { base["keyTransformer"] = value; }
		}

		/// <summary>
		/// Gets or sets the type of the <see cref="T:Enyim.Caching.Memcached.IMemcachedNodeLocator"/> which will be used to assign items to Memcached nodes.
		/// </summary>
		[ConfigurationProperty("nodeLocator", IsRequired = false), TypeConverter(typeof(TypeNameConverter)), InterfaceValidator(typeof(Enyim.Caching.Memcached.IMemcachedNodeLocator))]
		public Type NodeLocator
		{
			get { return (Type)base["nodeLocator"]; }
			set { base["nodeLocator"] = value; }
		}

		/// <summary>
		/// Gets or sets the type of the <see cref="T:Enyim.Caching.Memcached.ITranscoder"/> which will be used serialzie or deserialize items.
		/// </summary>
		[ConfigurationProperty("transcoder", IsRequired = false), TypeConverter(typeof(TypeNameConverter)), InterfaceValidator(typeof(Enyim.Caching.Memcached.ITranscoder))]
		public Type Transcoder
		{
			get { return (Type)base["transcoder"]; }
			set { base["transcoder"] = value; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether operation statistics are created using Windows Performance Counters.
		/// </summary>
		/// <remarks>This is set to false by default so the application using this library will work even if teh performance counters are not installed.</remarks>
		[ConfigurationProperty("enablePerformanceCounters", IsRequired = false, DefaultValue = false)]
		public bool EnablePerformanceCounters
		{
			get { return (bool)base["enablePerformanceCounters"]; }
			set { base["enablePerformanceCounters"] = value; }
		}

		/// <summary>
		/// Called after deserialization.
		/// </summary>
		protected override void PostDeserialize()
		{
			WebContext hostingContext = base.EvaluationContext.HostingContext as WebContext;

			if (hostingContext != null && hostingContext.ApplicationLevel == WebApplicationLevel.BelowApplication)
			{
				throw new InvalidOperationException("The " + this.SectionInformation.SectionName + " section cannot be defined below the application level.");
			}
		}

		/// <summary>
		/// Gets or sets the type of the communication between client and server.
		/// </summary>
		[ConfigurationProperty("protocol", IsRequired = false, DefaultValue = MemcachedProtocol.Text)]
		public MemcachedProtocol Protocol
		{
			get { return (MemcachedProtocol)base["protocol"]; }
			set { base["protocol"] = value; }
		}

		#region [ IMemcachedClientConfiguration]
		IList<IPEndPoint> IMemcachedClientConfiguration.Servers
		{
			get { return this.Servers.ToIPEndPointCollection(); }
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

		IAuthenticationConfiguration IMemcachedClientConfiguration.Authentication
		{
			get { return this.Authentication; }
		}

		bool IMemcachedClientConfiguration.EnablePerformanceCounters
		{
			get { return this.EnablePerformanceCounters; }
			set { this.EnablePerformanceCounters = value; }
		}

		MemcachedProtocol IMemcachedClientConfiguration.Protocol
		{
			get { return this.Protocol; }
			set { this.Protocol = value; }
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