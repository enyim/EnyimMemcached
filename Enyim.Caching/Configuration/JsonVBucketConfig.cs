//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Security.Cryptography;
//using System.Net;
//using System.Web.Script.Serialization;
//using Enyim.Caching.Configuration;

//namespace Enyim.Caching.Configuration
//{
//    /// <summary>
//    /// Parses a json formatted vbucket config.
//    /// </summary>
//    public class JsonVBucketConfig : IVBucketConfiguration
//    {
//        private Func<HashAlgorithm> factory;
//        private IPEndPoint[] servers;
//        private VBucket[] buckets;

//        public JsonVBucketConfig(string json)
//        {
//            var config = new JavaScriptSerializer().Deserialize<_JsonConfig>(json);

//            if (config.numReplicas < 0)
//                throw new ArgumentException("Invalid numReplicas: " + config.numReplicas, "json");

//            if (hashFactory.TryGetValue(config.hashAlgorithm, out this.factory))
//                throw new ArgumentException("Unknown hash algorithm: " + config.hashAlgorithm, "json");

//            this.servers = config.serverList.Select(endpoint => ConfigurationHelper.ResolveToEndPoint(endpoint)).ToArray();
//            this.buckets = config.vBucketMap.Select((bucket, index) =>
//                            {
//                                if (bucket == null || bucket.Length != config.numReplicas + 1)
//                                    throw new ArgumentException("Invalid bucket definition at index " + index, "json");

//                                return new VBucket(bucket[0], bucket.Skip(1).Take(config.numReplicas)/* .Where(v => v > -1) */.ToArray());
//                            }).ToArray();
//        }

//        #region [ _JsonConfig                  ]

//        private class _JsonConfig
//        {
//            public string hashAlgorithm;
//            public int numReplicas;
//            public string[] serverList;
//            public int[][] vBucketMap;
//        }

//        #endregion
//        #region [ IVBucketConfiguration        ]

//        HashAlgorithm IVBucketConfiguration.CreateHashAlgorithm()
//        {
//            return factory();
//        }

//        IList<IPEndPoint> IVBucketConfiguration.Servers
//        {
//            get { return this.servers; }
//        }

//        IList<VBucket> IVBucketConfiguration.Buckets
//        {
//            get { return this.buckets; }
//        }

//        #endregion
//        #region [ hashFactory                  ]

//        private static readonly Dictionary<string, Func<HashAlgorithm>> hashFactory = new Dictionary<string, Func<HashAlgorithm>>(StringComparer.OrdinalIgnoreCase)
//        {
//            { String.Empty, () => new HashkitOneAtATime() },
//            { "default", () => new HashkitOneAtATime() },
//            { "crc", () => new HashkitCrc32() },
//            { "fnv1_32", () => new Enyim.FNV1() },
//            { "fnv1_64", () => new Enyim.FNV1a() },
//            { "fnv1a_32", () => new Enyim.FNV64() },
//            { "fnv1a_64", () => new Enyim.FNV64a() },
//            { "murmur", () => new HashkitMurmur() }
//        };

//        #endregion
//    }
//}
