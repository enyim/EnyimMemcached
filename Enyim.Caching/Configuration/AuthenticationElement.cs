using System;
using System.ComponentModel;
using System.Configuration;
using System.Collections.Generic;

namespace Enyim.Caching.Configuration
{
	/// <summary>
	/// Configures the authentication settings for Memcached servers.
	/// </summary>
	public sealed class AuthenticationElement : ConfigurationElement, IAuthenticationConfiguration
	{
		// TODO make this element play nice with the configuratino system (allow saving, etc.)
		private Dictionary<string, object> parameters = new Dictionary<string, object>();

		/// <summary>
		/// Gets or sets the type of the <see cref="T:Enyim.Caching.Memcached.IAuthenticationProvider"/> which will be used authehticate the connections to the Memcached nodes.
		/// </summary>
		[ConfigurationProperty("type", IsRequired = false), TypeConverter(typeof(TypeNameConverter)), InterfaceValidator(typeof(Enyim.Caching.Memcached.ISaslAuthenticationProvider))]
		public Type Type
		{
			get { return (Type)base["type"]; }
			set { base["type"] = value; }
		}

		protected override bool OnDeserializeUnrecognizedAttribute(string name, string value)
		{
			ConfigurationProperty property = new ConfigurationProperty(name, typeof(string), value);
			base[property] = value;

			this.parameters[name] = value;

			return true;
		}

		#region [ IAuthenticationConfiguration ]

		Type IAuthenticationConfiguration.Type
		{
			get { return this.Type; }
			set
			{
				ConfigurationHelper.CheckForInterface(value, typeof(Enyim.Caching.Memcached.ISaslAuthenticationProvider));

				this.Type = value;
			}
		}

		System.Collections.Generic.Dictionary<string, object> IAuthenticationConfiguration.Parameters
		{
			// HACK we should return a clone, but i'm lazy now
			get { return this.parameters; }
		}

		#endregion
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
