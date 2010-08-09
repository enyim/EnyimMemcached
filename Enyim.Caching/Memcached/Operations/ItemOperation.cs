//using System;
//using System.Diagnostics;

//namespace Enyim.Caching.Memcached.Operations
//{
//    /// <summary>
//    /// Base class for implementing operations working with keyed items. Handles server selection based on item key.
//    /// </summary>
//    internal abstract class ItemOperation : Operation
//    {
//        private string key;
//        private string hashedKey;
//        private ulong cas;

//        private PooledSocket socket;

//        protected ItemOperation(IServerPool pool, string key)
//            : base(pool)
//        {
//            if (key == null) throw new ArgumentNullException("key", "Item key must be specified.");
//            if (key.Length == 0) throw new ArgumentException("Item key must be specified.", "key");

//            this.key = key;
//        }

//        protected string Key
//        {
//            get { return this.key; }
//        }

//        public ulong Cas
//        {
//            get { return this.cas; }
//            set { this.cas = value; }
//        }

//        /// <summary>
//        /// Gets the hashed version of the key which should be used as key in communication with memcached
//        /// </summary>
//        protected string HashedKey
//        {
//            get
//            {
//                if (this.hashedKey == null)
//                {
//                    string tmp = this.ServerPool.KeyTransformer.Transform(this.key);
//                    Debug.Assert(!String.IsNullOrEmpty(tmp), this.ServerPool.KeyTransformer + " just returned an empty key.");

//                    this.hashedKey = tmp;
//                }

//                return this.hashedKey;
//            }
//        }

//        protected PooledSocket Socket
//        {
//            get
//            {
//                if (this.socket == null)
//                {
//                    // get a connection to the server which the "key" belongs to
//                    PooledSocket ps = this.ServerPool.Acquire(this.key);

//                    // null was returned, so our server is dead and no one could replace it
//                    // (probably all of our servers are down)
//                    if (ps == null)
//                        return null;

//                    this.socket = ps;
//                }

//                return this.socket;
//            }
//        }

//        public override void Dispose()
//        {
//            GC.SuppressFinalize(this);

//            if (this.socket != null)
//            {
//                ((IDisposable)this.socket).Dispose();
//                this.socket = null;
//            }

//            base.Dispose();
//        }
//    }
//}

//#region [ License information          ]
///* ************************************************************
// * 
// *    Copyright (c) 2010 Attila Kiskó, enyim.com
// *    
// *    Licensed under the Apache License, Version 2.0 (the "License");
// *    you may not use this file except in compliance with the License.
// *    You may obtain a copy of the License at
// *    
// *        http://www.apache.org/licenses/LICENSE-2.0
// *    
// *    Unless required by applicable law or agreed to in writing, software
// *    distributed under the License is distributed on an "AS IS" BASIS,
// *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// *    See the License for the specific language governing permissions and
// *    limitations under the License.
// *    
// * ************************************************************/
//#endregion
