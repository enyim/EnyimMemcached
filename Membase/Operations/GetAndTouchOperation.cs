using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enyim.Caching.Memcached.Protocol.Binary;
using Enyim.Caching.Memcached;
using System.IO;
using System.Threading;
using Enyim.Caching;

namespace Membase
{
	internal class GetAndTouchOperation : GetOperation, IGetAndTouchOperation, IOperationWithState
	{
		private static readonly Enyim.Caching.ILog log = Enyim.Caching.LogManager.GetLogger(typeof(GetAndTouchOperation));

		private uint newExpiration;
		private OperationState state;
		private VBucketNodeLocator locator;

		public GetAndTouchOperation(VBucketNodeLocator locator, string key, uint newExpiration)
			: base(key)
		{
			this.locator = locator;
			this.newExpiration = newExpiration;
		}

		protected override BinaryRequest Build()
		{
			var retval = base.Build();
			retval.Operation = 0x1d;

			if (this.locator != null)
			{
				retval.Reserved = (ushort)locator.GetIndex(this.Key);

				if (log.IsDebugEnabled) log.DebugFormat("Key {0} was mapped to {1}", this.Key, retval.Reserved);
			}

			var extra = new byte[4];

			BinaryConverter.EncodeUInt32(this.newExpiration, extra, 0);
			retval.Extra = new ArraySegment<byte>(extra);

			return retval;
		}

		protected override bool ProcessResponse(BinaryResponse response)
		{
			var r = base.ProcessResponse(response);

			if (this.locator != null &&
				!VBucketAwareOperationFactory.GuessResponseState(response, out this.state))
				return false;

			return r;
		}

		#region [ IOperationWithState          ]

		OperationState IOperationWithState.State
		{
			get { return this.state; }
		}

		#endregion
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
