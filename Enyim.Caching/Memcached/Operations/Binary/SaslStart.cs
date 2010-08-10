using System;
using System.Collections.Generic;
using System.Text;

namespace Enyim.Caching.Memcached.Operations.Binary
{
	/// <summary>
	/// Starts the SASL auth sequence.
	/// </summary>
	internal class SaslStart : SaslStep
	{
		public SaslStart(ISaslAuthenticationProvider provider) : base(provider) { }

		protected internal override IList<ArraySegment<byte>> GetBuffer()
		{
			// create a Sasl Start command
			var request = new BinaryRequest(OpCode.SaslStart)
			{
				Key = this.Provider.Type,
				Data = new ArraySegment<byte>(this.Provider.Authenticate())
			};

			return request.CreateBuffer();
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
