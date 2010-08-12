using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Net;
using System.Web.Configuration;
using Enyim.Caching.Configuration;
using Enyim.Caching.Memcached;

namespace NorthScale.Store.Configuration
{
	/// <summary>
	/// Configures the <see cref="T:MemcachedClient"/>. This class cannot be inherited.
	/// </summary>
	public sealed class NorthScaleClientSection : ConfigurationSection, INorthScaleClientConfiguration
	{
		[ConfigurationProperty("servers", IsRequired = true)]
		public ServersElement Servers
		{
			get { return (ServersElement)base["servers"]; }
			set { base["servers"] = value; }
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
		/// Gets or sets the <see cref="T:Enyim.Caching.Memcached.IMemcachedNodeLocator"/> which will be used to assign items to Memcached nodes.
		/// </summary>
		[ConfigurationProperty("locator", IsRequired = false)]
		public ProviderElement<IMemcachedNodeLocator> NodeLocator
		{
			get { return (ProviderElement<IMemcachedNodeLocator>)base["locator"]; }
			set { base["locator"] = value; }
		}

		/// <summary>
		/// Gets or sets the <see cref="T:Enyim.Caching.Memcached.IMemcachedKeyTransformer"/> which will be used to convert item keys for Memcached.
		/// </summary>
		[ConfigurationProperty("keyTransformer", IsRequired = false)]
		public ProviderElement<IMemcachedKeyTransformer> KeyTransformer
		{
			get { return (ProviderElement<IMemcachedKeyTransformer>)base["keyTransformer"]; }
			set { base["keyTransformer"] = value; }
		}

		/// <summary>
		/// Gets or sets the <see cref="T:Enyim.Caching.Memcached.ITranscoder"/> which will be used serialzie or deserialize items.
		/// </summary>
		[ConfigurationProperty("transcoder", IsRequired = false)]
		public ProviderElement<ITranscoder> Transcoder
		{
			get { return (ProviderElement<ITranscoder>)base["transcoder"]; }
			set { base["transcoder"] = value; }
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

		#region [ interface                     ]
		IList<Uri> INorthScaleClientConfiguration.Urls
		{
			get { return this.Servers.Urls.ToUriCollection(); }
		}

		ISocketPoolConfiguration INorthScaleClientConfiguration.SocketPool
		{
			get { return this.SocketPool; }
		}

		IMemcachedKeyTransformer INorthScaleClientConfiguration.CreateKeyTransformer()
		{
			return this.KeyTransformer.CreateInstance() ?? new DefaultKeyTransformer();
		}

		IMemcachedNodeLocator INorthScaleClientConfiguration.CreateNodeLocator()
		{
			return this.NodeLocator.CreateInstance() ?? new KetamaNodeLocator();
		}

		ITranscoder INorthScaleClientConfiguration.CreateTranscoder()
		{
			return this.Transcoder.CreateInstance() ?? new DefaultTranscoder();
		}

		NetworkCredential INorthScaleClientConfiguration.Credentials
		{
			get
			{
				var srv = this.Servers;
				if (String.IsNullOrEmpty(srv.UserName)) return null;

				return new System.Net.NetworkCredential(srv.UserName, srv.Password);
			}
		}

		string INorthScaleClientConfiguration.Bucket
		{
			get { return this.Servers.Bucket; }
		}

		BucketPortType INorthScaleClientConfiguration.Port
		{
			get { return this.Servers.Port; }
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
