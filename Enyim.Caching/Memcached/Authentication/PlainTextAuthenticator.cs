using System;
using System.Collections.Generic;
using System.Text;

namespace Enyim.Caching.Memcached
{
	/// <summary>
	/// Implements the default plain text ("PLAIN") Memcached authentication. It expects "userName" and "password" parameters during initalization.
	/// </summary>
	public sealed class PlainTextAuthenticator : ISaslAuthenticationProvider
	{
		private byte[] authData;

		public PlainTextAuthenticator() { }
		public PlainTextAuthenticator(string userName, string password) 
		{
			this.authData = CreateAuthData(userName, password);
		}

		string ISaslAuthenticationProvider.Type
		{
			get { return "PLAIN"; }
		}

		void ISaslAuthenticationProvider.Initialize(Dictionary<string, object> parameters)
		{
			string userName = (string)parameters["userName"];
			string password = (string)parameters["password"];

			this.authData = CreateAuthData(userName, password);
		}

		byte[] ISaslAuthenticationProvider.Authenticate()
		{
			return this.authData;
		}

		byte[] ISaslAuthenticationProvider.Continue(byte[] data)
		{
			return null;
		}

		private static byte[] CreateAuthData(string userName, string password)
		{
			return System.Text.Encoding.UTF8.GetBytes("memcached\0" + userName + "\0" + password);
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
