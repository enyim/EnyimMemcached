using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Enyim.Caching.Memcached.Operations.Binary
{
	internal class BinaryRequest
	{
		private static int InstanceCounter;

		public OpCode Operation;
		public string Key;
		public ulong Cas;
		public int SessionId;

		public BinaryRequest(OpCode operation)
		{
			this.Operation = operation;
		}

		public unsafe IList<ArraySegment<byte>> CreateBuffer()
		{
			// key size 
			int keyLength = this.Key == null ? 0 : this.Key.Length;
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

				// session id
				this.SessionId = Interlocked.Increment(ref InstanceCounter);

				buffer[0x0c] = (byte)(this.SessionId >> 24);
				buffer[0x0d] = (byte)(this.SessionId >> 16);
				buffer[0x0e] = (byte)(this.SessionId >> 8);
				buffer[0x0f] = (byte)(this.SessionId & 255);

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
			if (keyLength > 0) retval.Add(new ArraySegment<byte>(Encoding.ASCII.GetBytes(this.Key)));
			if (bodyLength > 0) retval.Add(body);

			return retval;
		}

		public ArraySegment<byte> Extra;
		public ArraySegment<byte> Data;

		public void Write(PooledSocket socket)
		{
			socket.Write(this.CreateBuffer());
		}
	}
}
