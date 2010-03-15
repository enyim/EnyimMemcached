using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Enyim.Caching.Memcached.Operations.Binary
{
	internal class BinaryRequest
	{
		private static log4net.ILog log = log4net.LogManager.GetLogger(typeof(BinaryRequest));
		private static int InstanceCounter;

		public OpCode Operation;
		public string Key;
		public ulong Cas;
		public readonly int CorrelationId;

		public BinaryRequest(OpCode operation)
		{
			this.Operation = operation;

			// session id
			this.CorrelationId = Interlocked.Increment(ref InstanceCounter);
		}

		public unsafe IList<ArraySegment<byte>> CreateBuffer()
		{
			// key size 
			byte[] keyData = BinaryConverter.EncodeKey(this.Key);
			int keyLength = keyData == null ? 0 : keyData.Length;

			if (keyLength > 0xffff) throw new InvalidOperationException("KeyTooLong");

			// extra size
			ArraySegment<byte> extras = this.Extra;
			int extraLength = extras.Array == null ? 0 : extras.Count;
			if (extraLength > 0xff) throw new InvalidOperationException("ExtraTooLong");

			// body size
			ArraySegment<byte> body = this.Data;
			int bodyLength = body.Array == null ? 0 : body.Count;
			if (bodyLength > 1024 * 1024) throw new InvalidOperationException("BodyTooLong");

			// total payload size
			int totalLength = extraLength + keyLength + bodyLength;

			//build the header
			byte[] header = new byte[24];

			fixed (byte* buffer = header)
			{
				buffer[0x00] = 0x80; // magic
				buffer[0x01] = (byte)this.Operation;

				// key length
				buffer[0x02] = (byte)(keyLength >> 8);
				buffer[0x03] = (byte)(keyLength & 255);

				// extra length
				buffer[0x04] = (byte)(extraLength);

				// 5 -- data type, 0 (RAW)
				// 6,7 -- reserved, always 0

				// body length
				buffer[0x08] = (byte)(totalLength >> 24);
				buffer[0x09] = (byte)(totalLength >> 16);
				buffer[0x0a] = (byte)(totalLength >> 8);
				buffer[0x0b] = (byte)(totalLength & 255);

				buffer[0x0c] = (byte)(this.CorrelationId >> 24);
				buffer[0x0d] = (byte)(this.CorrelationId >> 16);
				buffer[0x0e] = (byte)(this.CorrelationId >> 8);
				buffer[0x0f] = (byte)(this.CorrelationId & 255);

				ulong cas = this.Cas;
				// CAS
				if (cas > 0)
				{
					// skip this if no session id is specfied
					buffer[0x10] = (byte)(cas >> 56);
					buffer[0x11] = (byte)(cas >> 48);
					buffer[0x12] = (byte)(cas >> 40);
					buffer[0x13] = (byte)(cas >> 32);
					buffer[0x14] = (byte)(cas >> 24);
					buffer[0x15] = (byte)(cas >> 16);
					buffer[0x16] = (byte)(cas >> 8);
					buffer[0x17] = (byte)(cas & 255);
				}
			}

			List<ArraySegment<byte>> retval = new List<ArraySegment<byte>>(4);

			retval.Add(new ArraySegment<byte>(header));

			if (extraLength > 0) retval.Add(extras);

			// NOTE key must be already encoded and should not contain any invalid characters whihc are not allowed by the protocol
			if (keyLength > 0) retval.Add(new ArraySegment<byte>(keyData));
			if (bodyLength > 0) retval.Add(body);

			return retval;
		}

		public ArraySegment<byte> Extra;
		public ArraySegment<byte> Data;

		public void Write(PooledSocket socket)
		{
			IList<ArraySegment<byte>> buffer = this.CreateBuffer();
#if DEBUG_PROTOCOL
			if (log.IsDebugEnabled)
			{
				log.Debug("Sending binary request");
				ArraySegment<byte> header = buffer[0];

				StringBuilder sb = new StringBuilder(128).AppendLine();

				for (int i = 0; i < header.Count; i++)
				{
					byte value = header.Array[i + header.Offset];
					sb.Append(value < 16 ? "0x0" : "0x").Append(value.ToString("X"));

					if (i % 4 == 3) sb.AppendLine(); else sb.Append(" ");
				}

				log.Debug(sb.ToString());
			}
#endif
			socket.Write(buffer);
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