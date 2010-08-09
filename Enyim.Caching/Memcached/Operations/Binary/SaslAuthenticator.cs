using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace Enyim.Caching.Memcached.Operations.Binary
{
	internal class BinaryAuthenticator : IAuthenticator
	{
		private static log4net.ILog log = log4net.LogManager.GetLogger(typeof(BinaryAuthenticator));

		private ISaslAuthenticationProvider provider;

		public BinaryAuthenticator(ISaslAuthenticationProvider provider)
		{
			if (provider == null) throw new ArgumentNullException("provider");

			this.provider = provider;
		}

		public bool Authenticate(PooledSocket socket)
		{
			return Authenticate(socket, this.provider);
		}

		public static bool Authenticate(PooledSocket socket, ISaslAuthenticationProvider provider)
		{
			if (log.IsDebugEnabled)
				log.DebugFormat("Authenticating socket {0} using provider {1}", socket.InstanceId, provider.GetType());

			// create a Sasl Start command
			BinaryRequest request = new BinaryRequest(OpCode.SaslStart);
			request.Key = provider.Type;

			// set the auth data
			request.Data = new ArraySegment<byte>(provider.Authenticate());
			request.Write(socket);

			// read the response
			BinaryResponse response = new BinaryResponse();

			// auth step failed, or further steps are needed
			while (!response.Read(socket))
			{
				// challenge-response authentication
				if (response.StatusCode == 0x21)
				{
					request = new BinaryRequest(OpCode.SaslStep);
					request.Key = provider.Type;

					// set the auth data
					// we're cutting a corner here with the Data.Array, it always contains the full body
					request.Data = new ArraySegment<byte>(provider.Continue(response.Data.Array));
					request.Write(socket);
				}
				else
				{
					if (log.IsWarnEnabled)
						log.WarnFormat("Authentication failed, return code: 0x{0:x}", response.StatusCode);

					// invalid credentials or other error
					return false;
				}
			}

			return true;
		}

		bool IAuthenticator.Authenticate(PooledSocket socket)
		{
			return this.Authenticate(socket);
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
