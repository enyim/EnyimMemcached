using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using System.IO;
using System.Text.RegularExpressions;

namespace Membase
{
	internal class ConfigHelper
	{
		private static readonly Enyim.Caching.ILog log = Enyim.Caching.LogManager.GetLogger(typeof(ConfigHelper));

		private WebClientWithTimeout wcwt;

		public ConfigHelper(WebClientWithTimeout client)
		{
			this.wcwt = client;
		}

		/// <summary>
		/// Deserializes the content of an url as a json object
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="uri"></param>
		/// <returns></returns>
		private T DeserializeUri<T>(Uri uri)
		{
			var cred = this.wcwt.Credentials;

			if (cred == null)
			{
				if (log.IsDebugEnabled) log.Debug("No credentials are specified, skipping the Authorization header.");
			}
			else
			{
				var nc = cred.GetCredential(uri, "Basic");
				if (nc == null)
				{
					if (log.IsDebugEnabled) log.DebugFormat("Cannot append Authorization header, the client did not gave us a credential for this url: {0}.", uri);
				}
				else
				{
					// we'll use the bucket name/password passed by the client for authentication
					// (the default bucket's config data can be accessed anonymously)
					this.wcwt.Encoding = Encoding.UTF8;
					this.wcwt.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(nc.UserName + ":" + nc.Password));
				}
			}

			var info = this.wcwt.DownloadString(uri);
			var jss = new JavaScriptSerializer();

			return jss.Deserialize<T>(info);
		}

		private ClusterInfo GetClusterInfo(Uri clusterUrl)
		{
			var info = DeserializeUri<ClusterInfo>(clusterUrl);

			if (info == null)
				throw new ArgumentException("invalid pool url: " + clusterUrl);

			if (info.buckets == null || String.IsNullOrEmpty(info.buckets.uri))
				throw new ArgumentException("got an invalid response, missing { buckets : { uri : '' } }");

			return info;
		}

		/// <summary>
		/// Asks the cluster for the specified bucket's configuration.
		/// </summary>
		/// <param name="poolUri"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public ClusterConfig ResolveBucket(Uri poolUri, string name)
		{
			var info = this.GetClusterInfo(poolUri);
			var root = new Uri(poolUri, info.buckets.uri);

			// first try the default auth mechanism: auth if 401, otherwise do nothing
			var allBuckets = this.DeserializeUri<ClusterConfig[]>(root);
			var retval = allBuckets.FirstOrDefault(b => b.name == name);

			if (retval == null)
			{
				if (log.IsWarnEnabled) log.WarnFormat("Could not find the pool '{0}' at {1}", name, poolUri);
			}
			else if (log.IsDebugEnabled) log.DebugFormat("Found config for bucket {0}.", name);

			return retval;
		}

		/// <summary>
		/// Finds the comet endpoint of the specified bucket. This checks all controller urls until one returns a config or all fails.
		/// </summary>
		/// <param name="pools"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public Uri[] GetBucketStreamingUris(Uri[] pools, string name)
		{
			if (pools == null) throw new ArgumentNullException("pools");
			if (pools.Length == 0) throw new ArgumentException("must specify at least one url", "pools");

			List<Uri> retval = new List<Uri>(pools.Length);

			for (var i = 0; i < pools.Length; i++)
			{
				try
				{
					var current = pools[i];
					var bucket = this.ResolveBucket(current, name);

					if (bucket != null)
						retval.Add(new Uri(current, bucket.streamingUri));
				}
				catch (Exception e)
				{
					log.Error(e);
				}
			}

			return retval.Count == 0 ? null : retval.ToArray();
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
