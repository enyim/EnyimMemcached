using System;
using System.Text;

namespace Enyim.Caching.Memcached.Protocol.Binary
{
    public static class BinaryConverter
    {
        public static ushort DecodeUInt16(Span<byte> buffer, int offset)
        {
            return (ushort)((buffer[offset] << 8) + buffer[offset + 1]);
        }

        public static int DecodeInt32(Span<byte> buffer, int offset)
        {
            var slice = buffer.Slice(offset);

            return (slice[0] << 24) | (slice[1] << 16) | (slice[2] << 8) | slice[3];
        }

        public static unsafe ulong DecodeUInt64(Span<byte> buffer, int offset)
        {
            var slice = buffer.Slice(offset);

            var part1 = (uint)((slice[0] << 24) | (slice[1] << 16) | (slice[2] << 8) | slice[3]);
            var part2 = (uint)((slice[4] << 24) | (slice[5] << 16) | (slice[6] << 8) | slice[7]);

            return ((ulong)part1 << 32) | part2;
        }

        public static unsafe void EncodeUInt16(uint value, Span<byte> buffer, int offset)
        {
            var slice = buffer.Slice(offset);

            slice[0] = (byte)(value >> 8);
            slice[1] = (byte)(value & 255);
        }

        public static unsafe void EncodeUInt32(uint value, Span<byte> buffer, int offset)
        {
            var slice = buffer.Slice(offset);

            slice[0] = (byte)(value >> 24);
            slice[1] = (byte)(value >> 16);
            slice[2] = (byte)(value >> 8);
            slice[3] = (byte)(value & 255);
        }

        public static unsafe void EncodeUInt64(ulong value, Span<byte> buffer, int offset)
        {
            var slice = buffer.Slice(offset);

            slice[0] = (byte)(value >> 56);
            slice[1] = (byte)(value >> 48);
            slice[2] = (byte)(value >> 40);
            slice[3] = (byte)(value >> 32);
            slice[4] = (byte)(value >> 24);
            slice[5] = (byte)(value >> 16);
            slice[6] = (byte)(value >> 8);
            slice[7] = (byte)(value & 255);
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
 *    Copyright (c) 2010 Attila Kisk? enyim.com
 *    
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *    
 *        http://www.apache.org/licenses/LICENSE-2.0
 *    
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *    
 * ************************************************************/
#endregion
