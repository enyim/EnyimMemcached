
namespace Enyim.Caching.Memcached.Operations.Text
{
	internal class GetOperation : ItemOperation
	{
		private object result;

		internal GetOperation(ServerPool pool, string key)
			: base(pool, key)
		{
		}

		public object Result
		{
			get { return this.result; }
		}

		protected override bool ExecuteAction()
		{
			PooledSocket socket = this.Socket;

			if (socket == null)
				return false;

			TextSocketHelper.SendCommand(socket, "get " + this.HashedKey);

			GetResponse r = GetHelper.ReadItem(socket);

			if (r == null)
			{
				socket.OwnerNode.PerfomanceCounters.LogGet(false);
			}
			else
			{
				this.result = this.ServerPool.Transcoder.Deserialize(r.Item);
				GetHelper.FinishCurrent(socket);

				socket.OwnerNode.PerfomanceCounters.LogGet(true);
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