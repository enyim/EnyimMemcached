using System;
using System.Globalization;

namespace Enyim.Caching.Memcached.Operations.Text
{
	internal sealed class DecrementOperation : ItemOperation
	{
		private ulong delta;
		private ulong result;

		internal DecrementOperation(IServerPool pool, string key, ulong delta)
			: base(pool, key)
		{
			this.delta = delta;
		}

		protected override bool ExecuteAction()
		{
			PooledSocket socket = this.Socket;
			if (socket == null)
				return false;

			TextSocketHelper.SendCommand(socket, String.Concat("decr ", this.HashedKey, " ", this.delta.ToString(CultureInfo.InvariantCulture)));

			string response = TextSocketHelper.ReadResponse(socket);

			//maybe we should throw an exception when the item is not found?
			if (String.Compare(response, "NOT_FOUND", StringComparison.Ordinal) == 0)
				return false;

			return UInt64.TryParse(response, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, CultureInfo.InvariantCulture, out this.result);
		}

		public ulong Result
		{
			get { return this.result; }
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
