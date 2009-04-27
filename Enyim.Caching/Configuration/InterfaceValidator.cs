using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;

namespace Enyim.Caching.Configuration
{
	internal sealed class InterfaceValidator : ConfigurationValidatorBase
	{
		private Type interfaceType;

		public InterfaceValidator(Type type)
		{
			if (!type.IsInterface)
				throw new ArgumentException(type + " must be an interface");

			this.interfaceType = type;
		}

		public override bool CanValidate(Type type)
		{
			return (type == typeof(Type)) || base.CanValidate(type);
		}

		public override void Validate(object value)
		{
			if (value != null)
				ConfigurationHelper.CheckForInterface((Type)value, this.interfaceType);
		}
	}

	internal sealed class InterfaceValidatorAttribute : ConfigurationValidatorAttribute
	{
		private Type interfaceType;

		public InterfaceValidatorAttribute(Type type)
		{
			if (!type.IsInterface)
				throw new ArgumentException(type + " must be an interface");

			this.interfaceType = type;
		}

		public override ConfigurationValidatorBase ValidatorInstance
		{
			get { return new InterfaceValidator(this.interfaceType); }
		}
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