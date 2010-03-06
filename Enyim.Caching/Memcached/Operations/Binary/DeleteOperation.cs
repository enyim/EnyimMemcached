
namespace Enyim.Caching.Memcached.Operations.Binary
{
	internal class DeleteOperation : ItemOperation
	{
		public DeleteOperation(ServerPool pool, string key) : base(pool, key) { }

		protected override bool ExecuteAction()
		{
			PooledSocket socket = this.Socket;
			if (socket == null) return false;

			BinaryRequest request = new BinaryRequest(OpCode.Delete) { Key = this.HashedKey };
			request.Write(this.Socket);

			BinaryResponse response = new BinaryResponse();
			response.Read(this.Socket);

			bool retval = response.Success;

			this.Socket.OwnerNode.PerfomanceCounters.LogDelete(retval);
			return retval;
		}
	}
}
