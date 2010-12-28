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
		private T DeserializeUri<T>(Uri uri, bool forceAuth)
		{
			if (forceAuth)
			{
				var cred = this.wcwt.Credentials;

				if (cred == null)
				{
					if (log.IsDebugEnabled) log.Debug("Cannot force basic auth, the client has no credentials specified.");
					return default(T);
				}

				var nc = cred.GetCredential(uri, "Basic");
				if (nc == null)
				{
					if (log.IsDebugEnabled) log.DebugFormat("Cannot force basic auth, the client did not gave us a credential for this url: {0}.", uri);
					return default(T);
				}

				// this will send the basic auth header even though the server did not ask for it
				// 1.6.4+ requires you to authenticate to get protected bucket info (but it does not give you a 401 error to force the auth)
				this.wcwt.Encoding = Encoding.UTF8;
				this.wcwt.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(nc.UserName + ":" + nc.Password));
			}

			var info = this.wcwt.DownloadString(uri);
			var jss = new JavaScriptSerializer();

			return jss.Deserialize<T>(info);
		}

		private ClusterInfo GetClusterInfo(Uri clusterUrl)
		{
			var info = DeserializeUri<ClusterInfo>(clusterUrl, false);

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
			var allBuckets = this.DeserializeUri<ClusterConfig[]>(root, false);
			var retval = allBuckets.FirstOrDefault(b => b.name == name);

			// we did not find the bucket
			if (retval == null)
			{
				if (log.IsDebugEnabled) log.DebugFormat("Could not find the pool '{0}' at {1}, trying with forceAuth=true", name, poolUri);

				// check if we're connecting to a 1.6.4 server
				var node = info.nodes == null ? null : info.nodes.FirstOrDefault();
				if (node == null)
				{
					if (log.IsDebugEnabled) log.Debug("No nodes are defined for the first bucket.");
				}
				else
				{
					// ignore git revisino and other garbage, only take x.y.z
					var m = Regex.Match(node.version, @"^\d+\.\d+\.\d+");
					if (!m.Success)
					{
						if (log.IsDebugEnabled) log.DebugFormat("Invalid version number: {0}", node.version);
					}
					else
					{
						var version = new Version(m.Value);

						// let's try to load the config with forced authentication
						if (version >= new Version(1, 6, 4))
						{
							allBuckets = this.DeserializeUri<ClusterConfig[]>(root, true);
							retval = allBuckets.FirstOrDefault(b => b.name == name);
						}
						else
							if (log.IsDebugEnabled) log.DebugFormat("This is a {0} server, skipping forceAuth.", node.version);
					}
				}

				if (retval == null)
				{
					if (log.IsWarnEnabled) log.WarnFormat("Could not find the pool '{0}' at {1}", name, poolUri);
				}
				else if (log.IsDebugEnabled) log.DebugFormat("Found config for bucket {0}.", name);
			}

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
