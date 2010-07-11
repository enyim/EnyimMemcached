using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace NorthScale.Store
{
	internal class WebClientWithTimeout : WebClient
	{
		public WebClientWithTimeout()
		{
			this.Encoding = Encoding.UTF8;

			this.Headers[HttpRequestHeader.CacheControl] = "no-cache";
			this.Headers[HttpRequestHeader.Accept] = "application/com.northscale.store+json";
			// TODO mayvbe we should version this
			this.Headers[HttpRequestHeader.UserAgent] = "enyim.com memcached client";
		}

		protected override WebRequest GetWebRequest(Uri address)
		{
			var retval = base.GetWebRequest(address);
			retval.Timeout = this.Timeout;

			var hrw = retval as HttpWebRequest;
			if (hrw != null)
				hrw.ReadWriteTimeout = this.ReadWriteTimeout;

			return retval;
		}

		public int ReadWriteTimeout { get; set; }
		public int Timeout { get; set; }
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
