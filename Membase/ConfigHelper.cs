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
	internal static class ConfigHelper
	{
		private static readonly Enyim.Caching.ILog log = Enyim.Caching.LogManager.GetLogger(typeof(ConfigHelper));

		/// <summary>
		/// Deserializes the content of an url as a json object
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="uri"></param>
		/// <returns></returns>
		private static T DeserializeUri<T>(WebClient client, Uri uri)
		{
			var info = client.DownloadString(uri);
			var jss = new JavaScriptSerializer();

			return jss.Deserialize<T>(info);
		}

		private static ClusterInfo GetClusterInfo(WebClient client, Uri clusterUrl)
		{
			var info = DeserializeUri<ClusterInfo>(client, clusterUrl);

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
		public static ClusterConfig ResolveBucket(WebClient client, Uri poolUri, string name)
		{
			var info = ConfigHelper.GetClusterInfo(client, poolUri);
			var root = new Uri(poolUri, info.buckets.uri);

			var allBuckets = ConfigHelper.DeserializeUri<ClusterConfig[]>(client, root);
			var retval = allBuckets.FirstOrDefault(b => b.name == name);

			if (retval == null)
			{
				if (log.IsWarnEnabled) log.WarnFormat("Could not find the pool '{0}' at {1}", name, poolUri);
			}
			else if (log.IsDebugEnabled) log.DebugFormat("Found config for bucket {0}.", name);

			return retval;
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
