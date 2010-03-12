using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Enyim.Caching.Memcached
{
	/// <summary>
	/// Default <see cref="T:ITranscoder"/> implementation. Primitive types are manually serialized, the rest is serialized using <see cref="T:BinarySerializer"/>.
	/// </summary>
	public sealed class DefaultTranscoder : ITranscoder
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

			TypeCode code = value == null ? TypeCode.Empty : Type.GetTypeCode(value.GetType());

			byte[] data;
			int length = -1;

			switch (code)
			{
				case TypeCode.Empty:
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
						new BinaryFormatter().Serialize(ms, value);

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

		object ITranscoder.Deserialize(CacheItem item)
		{
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
			
			if (code == TypeCode.Empty)
				return null;

			byte[] data = item.Data.Array;
			int offset = item.Data.Offset;
			int count = item.Data.Count;

			switch (code)
			{
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
						return new BinaryFormatter().Deserialize(ms);
					}

				default: throw new InvalidOperationException("Unknown TypeCode was returned: " + code);
			}
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