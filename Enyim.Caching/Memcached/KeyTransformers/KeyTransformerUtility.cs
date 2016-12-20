using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Enyim.Caching.Memcached.KeyTransformers
{
    public static class KeyTransformerUtility
    {
        public static string ToSHA1Hash(string key)
        {
            return new SHA1KeyTransformer().Transform(key);
        }
    }
}
