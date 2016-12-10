using System;
using System.Collections.Generic;
using System.Text;

namespace Enyim.Caching.Memcached
{
	/// <summary>
	/// Implements the default plain text ("PLAIN") Memcached authentication.
	/// </summary>
	/// <remarks>Either use the parametrized constructor, or pass the "userName" and "password" parameters during initalization.</remarks>
	public sealed class PlainTextAuthenticator : ISaslAuthenticationProvider
	{
		private byte[] authData;

		public PlainTextAuthenticator() { }

		public PlainTextAuthenticator(string zone, string userName, string password)
		{
			this.authData = CreateAuthData(zone, userName, password);
		}

		string ISaslAuthenticationProvider.Type
		{
			get { return "PLAIN"; }
		}

		void ISaslAuthenticationProvider.Initialize(Dictionary<string, object> parameters)
		{
			if (parameters != null)
			{
				this.authData = CreateAuthData(
                    GetParameter(parameters, "zone"), 
                    GetParameter(parameters, "userName"), 
                    GetParameter(parameters, "password"));
            }
		}

        private string GetParameter(Dictionary<string, object> parameters, string key)
        {
            if (parameters.ContainsKey(key))
            {
                return (string)parameters[key];
            }
            else
            {
                throw new MemcachedClientException($"Unable to find '{key}' authentication parameter for {nameof(PlainTextAuthenticator)}");
            }
        }

		byte[] ISaslAuthenticationProvider.Authenticate()
		{
			return this.authData;
		}

		byte[] ISaslAuthenticationProvider.Continue(byte[] data)
		{
			return null;
		}

		private static byte[] CreateAuthData(string zone, string userName, string password)
		{
			//message   = [authzid] UTF8NUL authcid UTF8NUL passwd
			//authcid   = 1*SAFE ; MUST accept up to 255 octets
			//authzid   = 1*SAFE ; MUST accept up to 255 octets
			//passwd    = 1*SAFE ; MUST accept up to 255 octets
			//UTF8NUL   = %x00 ; UTF-8 encoded NUL character
			return System.Text.Encoding.UTF8.GetBytes(zone + "\0" + userName + "\0" + password);
		}
	}
}

#region [ License information          ]
/* ************************************************************
 * 
 *    Copyright (c) 2010 Attila Kisk? enyim.com
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
