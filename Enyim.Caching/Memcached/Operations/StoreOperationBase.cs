//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Net.Sockets;
//using System.Net;
//using System.IO;
//using System.Security.Cryptography;
//using System.Globalization;
//using System.Threading;
//using System.Runtime.Serialization.Formatters.Binary;
//using System.IO.Compression;

//namespace Enyim.Caching.Memcached.Operations
//{
//    internal abstract class StoreOperationBase : ItemOperation
//    {
//        private const int MaxSeconds = 60 * 60 * 24 * 30;
//        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1);

//        private StoreCommand mode;
//        private object value;
//        private long expires;

//        public StoreOperationBase(ServerPool pool, StoreCommand mode, string key, object value, TimeSpan validFor, DateTime expiresAt)
//            : base(pool, key)
//        {
//            this.mode = mode;
//            this.value = value;
//            this.expires = GetExpiration(validFor, expiresAt);
//        }

//        private static long GetExpiration(TimeSpan validFor, DateTime expiresAt)
//        {
//            if (validFor >= TimeSpan.Zero && expiresAt > DateTime.MinValue)
//                throw new ArgumentException("You cannot specify both validFor and expiresAt.");

//            if (expiresAt > DateTime.MinValue)
//            {
//                if (expiresAt < UnixEpoch)
//                    throw new ArgumentOutOfRangeException("expiresAt", "expiresAt must be >= 1970/1/1");

//                return (long)(expiresAt.ToUniversalTime() - UnixEpoch).TotalSeconds;
//            }

//            if (validFor.TotalSeconds >= MaxSeconds || validFor < TimeSpan.Zero)
//                throw new ArgumentOutOfRangeException("validFor", "validFor must be < 30 days && >= 0");

//            return (long)validFor.TotalSeconds;
//        }

//        protected override bool ExecuteAction()
//        {
//            if (this.Socket == null)
//                return false;

//            CacheItem item = this.ServerPool.Transcoder.Serialize(this.value);

//            return this.Store(this.mode, item, this.expires);
//        }

//        protected abstract bool Store(StoreCommand mode, CacheItem item, long expires);
//    }
//}

//#region [ License information          ]
///* ************************************************************
// *
// * Copyright (c) Attila Kiskó, enyim.com
// *
// * This source code is subject to terms and conditions of 
// * Microsoft Permissive License (Ms-PL).
// * 
// * A copy of the license can be found in the License.html
// * file at the root of this distribution. If you can not 
// * locate the License, please send an email to a@enyim.com
// * 
// * By using this source code in any fashion, you are 
// * agreeing to be bound by the terms of the Microsoft 
// * Permissive License.
// *
// * You must not remove this notice, or any other, from this
// * software.
// *
// * ************************************************************/
//#endregion