
namespace Enyim.Caching.Memcached.Operations.Binary
{
	internal class GetOperation : ItemOperation
	{
		public GetOperation(ServerPool pool, string key) : base(pool, key) { }

		protected override bool ExecuteAction()
		{
			PooledSocket socket = this.Socket;
			if (socket == null) return false;

			BinaryRequest request = new BinaryRequest(OpCode.Get);
			request.Key = this.HashedKey;
			request.Write(this.Socket);

			BinaryResponse response = new BinaryResponse();

			if (response.Read(this.Socket))
			{
				int flags = BinaryConverter.DecodeInt32(response.Extra, 0);
				this.result = this.ServerPool.Transcoder.Deserialize(new CacheItem((ushort)flags, response.Data));
				this.Socket.OwnerNode.PerfomanceCounters.LogGet(true);

				return true;
			}

			this.Socket.OwnerNode.PerfomanceCounters.LogGet(false);
			return false;
		}

		private object result;

		public object Result
		{
			get { return this.result; }
		}

	}
}
