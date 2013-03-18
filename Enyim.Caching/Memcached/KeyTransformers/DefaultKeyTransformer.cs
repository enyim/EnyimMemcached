using System;
using System.Text;

namespace Enyim.Caching.Memcached
{
	/// <summary>
	/// A transformer that simply returns the original key.
	/// A valid UTF-8 key is recommended.
	/// </summary>
	public class DefaultKeyTransformer : KeyTransformerBase
	{
		public override string Transform(string key)
		{
			//default behavior is to allow all valid strings
			return key;
		}
	}
}

#region [ License information          ]
/* ************************************************************
 *
 *    @author Couchbase <info@couchbase.com>
 *    @copyright 2012 Couchbase, Inc.
 *   Copyright (c) 2010 Attila Kisk�, enyim.com
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
