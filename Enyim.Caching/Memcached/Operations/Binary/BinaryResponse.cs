using System;

namespace Enyim.Caching.Memcached.Operations.Binary
{
	internal class BinaryResponse
	{
		private const byte MAGIC_VALUE = 0x81;
		private const int HEADER_OPCODE = 1;
		private const int HEADER_KEY = 2; // 2-3
		private const int HEADER_EXTRA = 4;
		private const int HEADER_DATATYPE = 5;
		private const int HEADER_STATUS = 6; // 6-7
		private const int HEADER_BODY = 8; // 8-11
		private const int HEADER_OPAQUE = 12; // 12-15
		private const int HEADER_CAS = 16; // 16-23

		public byte Opcode;
		public int KeyLength;
		public byte DataType;
		public int StatusCode;

		public uint Opaque;
		public ulong CAS;

		public ArraySegment<byte> Extra;
		public ArraySegment<byte> Data;

		public unsafe bool Read(PooledSocket socket)
		{
			if (!socket.IsAlive)
			{
				this.StatusCode = -1;
				return false;
			}

			byte[] header = new byte[24];
			socket.Read(header, 0, 24);

			fixed (byte* buffer = header)
			{
				if (buffer[0] != MAGIC_VALUE)
					throw new InvalidOperationException("Expected magic value " + MAGIC_VALUE + ", received: " + buffer[0]);

				int remaining = BinaryConverter.DecodeInt32(buffer, HEADER_BODY);
				int extraLength = buffer[HEADER_EXTRA];

				byte[] data = new byte[remaining];
				socket.Read(data, 0, remaining);

				this.Extra = new ArraySegment<byte>(data, 0, extraLength);
				this.Data = new ArraySegment<byte>(data, extraLength, data.Length - extraLength);

				this.DataType = buffer[HEADER_DATATYPE];
				this.Opcode = buffer[HEADER_OPCODE];
				this.StatusCode = BinaryConverter.DecodeInt16(buffer, HEADER_STATUS);

				this.KeyLength = BinaryConverter.DecodeInt16(buffer, HEADER_KEY);
				this.Opaque = (uint)BinaryConverter.DecodeInt32(buffer, HEADER_OPAQUE);
				this.CAS = BinaryConverter.DecodeUInt64(buffer, HEADER_CAS);
			}

			return this.StatusCode == 0;
		}
	}
}
