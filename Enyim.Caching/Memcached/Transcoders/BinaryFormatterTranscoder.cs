using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Enyim.Caching.Memcached.Transcoders
{
    public class BinaryFormatterTranscoder : DefaultTranscoder
    {
        protected override ArraySegment<byte> SerializeObject(object value)
        {
            using (var ms = new MemoryStream())
            {
                new BinaryFormatter().Serialize(ms, value);
                return new ArraySegment<byte>(ms.GetBuffer(), 0, (int)ms.Length);
            }
        }

        public override T Deserialize<T>(CacheItem item)
        {
            return (T)base.Deserialize(item);
        }

        protected override object DeserializeObject(ArraySegment<byte> value)
        {
            using (var ms = new MemoryStream(value.Array, value.Offset, value.Count))
            {
                return new BinaryFormatter().Deserialize(ms);
            }
        }
    }
}
