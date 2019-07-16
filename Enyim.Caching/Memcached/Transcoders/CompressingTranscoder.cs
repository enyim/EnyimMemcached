using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization;

namespace Enyim.Caching.Memcached.Transcoders
{
    public class CompressingTranscoder : DefaultTranscoder
    {
        protected override object DeserializeObject(ArraySegment<byte> value)
        {
            var ds = new NetDataContractSerializer();

            using (var ms = new MemoryStream(value.Array, value.Offset, value.Count))
            {
                using (var gs = new GZipStream(ms, CompressionMode.Decompress))
                {
                    return ds.Deserialize(gs);
                }
            }
        }

        protected override ArraySegment<byte> SerializeObject(object value)
        {
            using (var ms = new MemoryStream())
            {
                using (var gs = new GZipStream(ms, CompressionMode.Compress, true))
                {
                    new NetDataContractSerializer().Serialize(gs, value);
                }

                return new ArraySegment<byte>(ms.GetBuffer(), 0, (int) ms.Length);
            }
        }
    }
}
