using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace NorthScale.Store
{
	internal class ConfigHelper
	{
		private static log4net.ILog log = log4net.LogManager.GetLogger(typeof(ConfigHelper));

		private WebClient client;

		public ConfigHelper() { }

		public ICredentials Credentials { get; set; }

		/// <summary>
		/// Creates a WebClient for communicating with the REST API.
		/// </summary>
		/// <param name="credentials"></param>
		/// <returns></returns>
		public static WebClient CreateClient(ICredentials credentials)
		{
			var retval = new WebClient();

			retval.Credentials = credentials;

			retval.Headers[HttpRequestHeader.CacheControl] = "no-cache";
			retval.Headers[HttpRequestHeader.Accept] = "application/com.northscale.store+json";
			// TODO mayvbe we should version this
			retval.Headers[HttpRequestHeader.UserAgent] = "enyim.com memcached client";

			retval.Encoding = Encoding.UTF8;

			return retval;
		}

		private WebClient GetWebClient()
		{
			if (this.client == null)
			{
				this.client = CreateClient(this.Credentials);
			}

			return this.client;
		}

		private T DeserializeUri<T>(Uri uri)
		{
			var info = this.GetWebClient().DownloadString(uri);
			var jss = new JavaScriptSerializer();

			return jss.Deserialize<T>(info);
		}

		private Uri GetBucketsRoot(Uri poolUri)
		{
			var poolInfo = DeserializeUri<Dictionary<string, object>>(poolUri);

			object tmp;
			Dictionary<string, object> dict;

			// get the buckets member whihc will hold the url of the bucket listing REST endpoint 
			if (!poolInfo.TryGetValue("buckets", out tmp)
				|| (dict = tmp as Dictionary<string, object>) == null)
				throw new ArgumentException("invalid pool url: " + poolUri);

			string bucketsUrl;

			// get the { uri: VALUE } part
			if (!dict.TryGetValue("uri", out tmp) || (bucketsUrl = tmp as string) == null)
				throw new ArgumentException("got an invalid response, missing { buckets : { uri : '' } }");

			return new Uri(poolUri, bucketsUrl);
		}

		private Bucket ResolveBucket(Uri poolUri, string name)
		{
			var root = this.GetBucketsRoot(poolUri);
			var allBuckets = this.DeserializeUri<Bucket[]>(root);

			return allBuckets.FirstOrDefault(b => b.name == name);
		}

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

		public BucketNode[] GetWorkingNodes(Uri[] pools, string name)
		{
			if (pools == null) throw new ArgumentNullException("pools");
			if (pools.Length == 0) throw new ArgumentException("must specify at least one url", "pools");

			for (var i = 0; i < pools.Length; i++)
			{
				try
				{
					var current = pools[i];
					var bucket = this.ResolveBucket(current, name);

					if (bucket != null)
						return bucket.nodes.Where(b => b.status == "healthy").ToArray();
				}
				catch (Exception e)
				{
					log.Error(e);
				}
			}

			return null;
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
