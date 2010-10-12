using System;
using System.Configuration;

namespace Membase.Configuration
{
	public sealed class UriValidatorAttribute : ConfigurationValidatorAttribute
	{
		public UriValidatorAttribute() { }

		public override ConfigurationValidatorBase ValidatorInstance
		{
			get { return new UriValidator(); }
		}

		#region [ UriValidator                 ]
		private class UriValidator : ConfigurationValidatorBase
		{
			public UriValidator() { }

			public override bool CanValidate(Type type)
			{
				return (type.TypeHandle.Equals(typeof(Uri).TypeHandle) || base.CanValidate(type));
			}

			public override void Validate(object value)
			{
				if (value != null && (value is string))
				{
					Uri tmp;

					if (!Uri.TryCreate((string)value, UriKind.Absolute, out tmp))
						throw new ConfigurationErrorsException(value + " must be an absolute url");

					if (tmp.Scheme != Uri.UriSchemeHttp)
						throw new ConfigurationErrorsException("only http is supported for now");
				}
			}
		}
		#endregion
	}

	public class UriConverter : ConfigurationConverterBase
	{
		public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
		{
			Uri tmp;

			if (Uri.TryCreate((string)value, UriKind.Absolute, out tmp))
				return tmp;

			return base.ConvertFrom(context, culture, value);
		}

		public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
		{
			if (value == null) return null;

			if (!(value is Uri))
				throw new InvalidOperationException("Unsupported type: " + value.GetType());

			return ((Uri)value).ToString();
		}

	}
}

#region [ License information          ]
/* ************************************************************
 * 
 *    Copyright (c) 2010 Attila Kiskó, enyim.com
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
