
namespace Enyim.Caching.Memcached.Operations.Binary
{
	internal class DeleteOperation : ItemOperation
	{
		public DeleteOperation(ServerPool pool, string key) : base(pool, key) { }

		protected override bool ExecuteAction()
		{
			var socket = this.Socket;
			if (socket == null) return false;

			var request = new BinaryRequest(OpCode.Delete) { Key = this.HashedKey };
			request.Write(this.Socket);

			var response = new BinaryResponse();
			response.Read(this.Socket);

			var retval = response.Success;

			this.Socket.OwnerNode.PerfomanceCounters.LogDelete(retval);
			return retval;
		}
	}
}
