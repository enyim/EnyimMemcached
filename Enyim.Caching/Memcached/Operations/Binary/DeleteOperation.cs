
namespace Enyim.Caching.Memcached.Operations.Binary
{
	internal class DeleteOperation : ItemOperation
	{
		public DeleteOperation(ServerPool pool, string key) : base(pool, key) { }

		protected override bool ExecuteAction()
		{
			PooledSocket socket = this.Socket;
			if (socket == null) return false;

			BinaryRequest request = new BinaryRequest(OpCode.Delete);
			request.Key = this.HashedKey;
			request.Write(this.Socket);

			BinaryResponse response = new BinaryResponse();

			bool retval = response.Read(this.Socket);

			this.Socket.OwnerNode.PerfomanceCounters.LogDelete(retval);
			return retval;
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