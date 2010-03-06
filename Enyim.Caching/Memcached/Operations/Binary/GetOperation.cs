
namespace Enyim.Caching.Memcached.Operations.Binary
{
	internal class GetOperation : ItemOperation
	{
		public GetOperation(ServerPool pool, string key) : base(pool, key) { }

		protected override bool ExecuteAction()
		{
			var socket = this.Socket;
			if (socket == null) return false;

			var request = new BinaryRequest(OpCode.Get) { Key = this.HashedKey };
			request.Write(this.Socket);

			var response = new BinaryResponse();
			response.Read(this.Socket);

			if (response.StatusCode != 0)
			{
				this.Socket.OwnerNode.PerfomanceCounters.LogGet(false);
				return false;
			}

			int flags = BinaryConverter.DecodeInt32(response.Extra, 0);
			this.result = this.ServerPool.Transcoder.Deserialize(new CacheItem((ushort)flags, response.Data));
			this.Socket.OwnerNode.PerfomanceCounters.LogGet(true);

			return true;
		}

		private object result;

		public object Result
		{
			get { return this.result; }
		}

	}
}
