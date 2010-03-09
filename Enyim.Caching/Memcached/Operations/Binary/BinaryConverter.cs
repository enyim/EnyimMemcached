using System;
using System.Text;

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

		public static byte[] EncodeKey(string key)
		{
			if (String.IsNullOrEmpty(key)) return null;

			return Encoding.UTF8.GetBytes(key);
		}

		public static string DecodeKey(byte[] data)
		{
			if (data == null || data.Length == 0) return null;

			return Encoding.UTF8.GetString(data);
		}

		public static string DecodeKey(byte[] data, int index, int count)
		{
			if (data == null || data.Length == 0 || count == 0) return null;

			return Encoding.UTF8.GetString(data, index, count);
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