using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace Enyim.Caching.Memcached.Operations.Binary
{
	internal class SaslAuthenticator 
	{
		private static log4net.ILog log = log4net.LogManager.GetLogger(typeof(SaslAuthenticator));

		private ISaslAuthenticationProvider provider;

		public SaslAuthenticator(ISaslAuthenticationProvider provider)
		{
			if (provider == null) throw new ArgumentNullException("provider");

			this.provider = provider;
		}

		public bool Authenticate(PooledSocket socket)
		{
			if (log.IsDebugEnabled)
				log.DebugFormat("Authenticating socket {0} using provider {1}", socket.InstanceId, this.provider.GetType());

			// create a Sasl Start command
			BinaryRequest request = new BinaryRequest(OpCode.SaslStart);
			request.Key = this.provider.Type;

			// set the auth data
			request.Data = new ArraySegment<byte>(this.provider.Authenticate());
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
					request.Key = this.provider.Type;

					// set the auth data
					// we're cutting a corner here with the Data.Array, it always contains the full body
					request.Data = new ArraySegment<byte>(this.provider.Continue(response.Data.Array));
					request.Write(socket);
				}
				else
				{
					if (log.IsDebugEnabled)
						log.DebugFormat("Authentication failed, return code: 0x{0:x}", response.StatusCode);

					// invalid credentials or other error
					return false;
				}
			}

			return true;
		}
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