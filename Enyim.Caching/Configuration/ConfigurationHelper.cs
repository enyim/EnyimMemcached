using System;

namespace Enyim.Caching.Configuration
{
	internal static class ConfigurationHelper
	{
		public static void CheckForInterface(Type type, Type interfaceType)
		{
			if (Array.IndexOf<Type>(type.GetInterfaces(), interfaceType) == -1)
				throw new System.Configuration.ConfigurationErrorsException("The type " + type.AssemblyQualifiedName + " must implement " + interfaceType.AssemblyQualifiedName);
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