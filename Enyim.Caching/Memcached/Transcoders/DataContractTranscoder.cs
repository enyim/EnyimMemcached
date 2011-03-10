using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace Enyim.Caching.Memcached
{
	/// <summary>
	/// Default <see cref="T:ITranscoder"/> implementation. Primitive types are manually serialized, the rest is serialized using <see cref="T:BinarySerializer"/>.
	/// </summary>
    public sealed class DataContractTranscoder : ITranscoder
	{
		internal const ushort RawDataFlag = 0xfa52;
		internal static readonly byte[] EmptyArray = new byte[0];

		CacheItem ITranscoder.Serialize(object value)
		{
			// raw data is a special case when some1 passes in a buffer (byte[] or ArraySegment<byte>)
			if (value is ArraySegment<byte>)
			{
				// ArraySegment<byte> is only passed in when a part of buffer is being 
				// serialized, usually from a MemoryStream (To avoid duplicating arrays 
				// the byte[] returned by MemoryStream.GetBuffer is placed into an ArraySegment.)
				// 
				return new CacheItem(RawDataFlag, (ArraySegment<byte>)value);
			}

			byte[] tmpByteArray = value as byte[];

			// - or we just received a byte[]. No further processing is needed.
			if (tmpByteArray != null)
			{
				return new CacheItem(RawDataFlag, new ArraySegment<byte>(tmpByteArray));
			}

			TypeCode code = value == null ? TypeCode.DBNull : Type.GetTypeCode(value.GetType());

			byte[] data;
			int length = -1;

			switch (code)
			{
				case TypeCode.DBNull:
					data = DefaultTranscoder.EmptyArray;
					length = 0;
					break;

				case TypeCode.String:
					data = Encoding.UTF8.GetBytes((string)value);
					break;

				case TypeCode.Boolean:
					data = BitConverter.GetBytes((bool)value);
					break;

				case TypeCode.Int16:
					data = BitConverter.GetBytes((short)value);
					break;

				case TypeCode.Int32:
					data = BitConverter.GetBytes((int)value);
					break;

				case TypeCode.Int64:
					data = BitConverter.GetBytes((long)value);
					break;

				case TypeCode.UInt16:
					data = BitConverter.GetBytes((ushort)value);
					break;

				case TypeCode.UInt32:
					data = BitConverter.GetBytes((uint)value);
					break;

				case TypeCode.UInt64:
					data = BitConverter.GetBytes((ulong)value);
					break;

				case TypeCode.Char:
					data = BitConverter.GetBytes((char)value);
					break;

				case TypeCode.DateTime:
					data = BitConverter.GetBytes(((DateTime)value).ToBinary());
					break;

				case TypeCode.Double:
					data = BitConverter.GetBytes((double)value);
					break;

				case TypeCode.Single:
					data = BitConverter.GetBytes((float)value);
					break;

				default:
					using (MemoryStream ms = new MemoryStream())
					{
                        new DataContractSerializer(value.GetType()).WriteObject(ms, value);

						code = TypeCode.Object;
						data = ms.GetBuffer();
						length = (int)ms.Length;
					}
					break;
			}

			if (length < 0)
				length = data.Length;

			return new CacheItem((ushort)((ushort)code | 0x0100), new ArraySegment<byte>(data, 0, length));
		}

		object ITranscoder.Deserialize<T>(CacheItem item)
		{
			if (item.Data == null || item.Data.Array == null)
				return null;

			if (item.Flags == RawDataFlag)
			{
				ArraySegment<byte> tmp = item.Data;

				if (tmp.Count == tmp.Array.Length)
					return tmp.Array;

				// we should never arrive here, but it's better to be safe than sorry
				byte[] retval = new byte[tmp.Count];

				Array.Copy(tmp.Array, tmp.Offset, retval, 0, tmp.Count);

				return retval;
			}

			TypeCode code = (TypeCode)(item.Flags & 0x00ff);

			byte[] data = item.Data.Array;
			int offset = item.Data.Offset;
			int count = item.Data.Count;

			switch (code)
			{
				// incrementing a non-existing key then getting it
				// returns as a string, but the flag will be 0
				// so treat all 0 flagged items as string
				// this may help inter-client data management as well
				//
				// however we store 'null' as Empty + an empty array, 
				// so this must special-cased for compatibilty with 
				// earlier versions. we introduced DBNull as null marker in emc2.6
				case TypeCode.Empty:
					return (data == null || count == 0)
							? null
							: Encoding.UTF8.GetString(data, offset, count);

				case TypeCode.DBNull:
					return null;

				case TypeCode.String:
					return Encoding.UTF8.GetString(data, offset, count);

				case TypeCode.Boolean:
					return BitConverter.ToBoolean(data, offset);

				case TypeCode.Int16:
					return BitConverter.ToInt16(data, offset);

				case TypeCode.Int32:
					return BitConverter.ToInt32(data, offset);

				case TypeCode.Int64:
					return BitConverter.ToInt64(data, offset);

				case TypeCode.UInt16:
					return BitConverter.ToUInt16(data, offset);

				case TypeCode.UInt32:
					return BitConverter.ToUInt32(data, offset);

				case TypeCode.UInt64:
					return BitConverter.ToUInt64(data, offset);

				case TypeCode.Char:
					return BitConverter.ToChar(data, offset);

				case TypeCode.DateTime:
					return DateTime.FromBinary(BitConverter.ToInt64(data, offset));

				case TypeCode.Double:
					return BitConverter.ToDouble(data, offset);

				case TypeCode.Single:
					return BitConverter.ToSingle(data, offset);

				case TypeCode.Object:
					using (MemoryStream ms = new MemoryStream(data, offset, count))
					{
                        ms.Seek(0, SeekOrigin.Begin);
                        DataContractSerializer ds = new DataContractSerializer(typeof(T));
                        return ds.ReadObject(ms);
					}

				default: throw new InvalidOperationException("Unknown TypeCode was returned: " + code);
			}
		}
	}
}

#region [ License information          ]
/* ************************************************************
 * 
 *    Copyright (c) 2010 Attila Kiskó, enyim.com
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
