using System;

namespace Enyim.Caching.Memcached.Operations.Binary
{
	internal static class BinaryConverter
	{
		public static unsafe int DecodeInt16(byte* buffer, int offset)
		{
			return ((int)(buffer[offset]) << 8) + buffer[offset + 1];
		}

		public static unsafe int DecodeInt32(ArraySegment<byte> segment, int offset)
		{
			fixed (byte* buffer = segment.Array)
			{
				byte* ptr = buffer + segment.Offset + offset;

				return DecodeInt32(buffer, 0);
			}
		}

		public static unsafe int DecodeInt32(byte* buffer, int offset)
		{
			buffer += offset;

			return (buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3];
		}
		public static unsafe ulong DecodeUInt64(byte[] buffer, int offset)
		{
			fixed (byte* ptr = buffer)
			{
				return DecodeUInt64(ptr, offset);
			}
		}

		public static unsafe ulong DecodeUInt64(byte* buffer, int offset)
		{
			buffer += offset;

			int part1 = (buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3];
			int part2 = (buffer[4] << 24) | (buffer[5] << 16) | (buffer[6] << 8) | buffer[7];

			return (ulong)(((long)part2) | (part1 << 32));
		}

		public static unsafe void EncodeUInt32(uint value, byte[] buffer, int offset)
		{
			fixed (byte* bufferPtr = buffer)
			{
				EncodeUInt32(value, bufferPtr, offset);
			}
		}

		public static unsafe void EncodeUInt32(uint value, byte* buffer, int offset)
		{
			byte* ptr = buffer + offset;

			ptr[0] = (byte)(value >> 24);
			ptr[1] = (byte)(value >> 16);
			ptr[2] = (byte)(value >> 8);
			ptr[3] = (byte)(value & 255);
		}

		public static unsafe void EncodeUInt64(ulong value, byte[] buffer, int offset)
		{
			fixed (byte* bufferPtr = buffer)
			{
				EncodeUInt64(value, bufferPtr, offset);
			}
		}

		public static unsafe void EncodeUInt64(ulong value, byte* buffer, int offset)
		{
			byte* ptr = buffer + offset;

			ptr[0] = (byte)(value >> 56);
			ptr[1] = (byte)(value >> 48);
			ptr[2] = (byte)(value >> 40);
			ptr[3] = (byte)(value >> 32);
			ptr[4] = (byte)(value >> 24);
			ptr[5] = (byte)(value >> 16);
			ptr[6] = (byte)(value >> 8);
			ptr[7] = (byte)(value & 255);
		}
	}
}
