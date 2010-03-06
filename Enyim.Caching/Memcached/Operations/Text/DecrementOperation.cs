using System;
using System.Globalization;

namespace Enyim.Caching.Memcached.Operations.Text
{
	internal sealed class DecrementOperation : ItemOperation, IMutatorOperation
	{
		private ulong delta;
		private ulong result;

		internal DecrementOperation(ServerPool pool, string key, ulong delta)
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