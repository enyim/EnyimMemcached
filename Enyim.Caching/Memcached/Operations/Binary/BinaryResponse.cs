using System;
using System.Text;

namespace Enyim.Caching.Memcached.Operations.Binary
{
	internal class BinaryResponse
	{
		private static log4net.ILog log = log4net.LogManager.GetLogger(typeof(BinaryResponse));

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

		public int CorrelationId;
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
#if DEBUG_PROTOCOL
			if (log.IsDebugEnabled)
			{
				log.Debug("Received binary response");

				StringBuilder sb = new StringBuilder(128).AppendLine();

				for (int i = 0; i < header.Length; i++)
				{
					byte value = header[i];
					sb.Append(value < 16 ? "0x0" : "0x").Append(value.ToString("X"));

					if (i % 4 == 3) sb.AppendLine(); else sb.Append(" ");
				}

				log.Debug(sb.ToString());
			}
#endif


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
				this.CorrelationId = BinaryConverter.DecodeInt32(buffer, HEADER_OPAQUE);
				this.CAS = BinaryConverter.DecodeUInt64(buffer, HEADER_CAS);
			}

			return this.StatusCode == 0;
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