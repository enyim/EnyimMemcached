using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enyim.Caching.Memcached.Protocol.Binary;
using System.Net;
using Enyim.Caching.Configuration;
using Enyim.Caching.Memcached;

namespace NorthScale.Store
{
	internal class VBucketAwareNode : BinaryNode
	{
		public VBucketAwareNode(IPEndPoint endpoint, ISocketPoolConfiguration config, ISaslAuthenticationProvider authenticationProvider)
			: base(endpoint, config, authenticationProvider) { }

		public int BucketIndex { get; set; }
	}
}
