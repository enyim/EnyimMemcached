using System;
using System.Collections.Generic;
using System.Text;

namespace Enyim.Reflection
{
	public interface IFastObjectFacory
	{
		object CreateInstance();
	}

	public interface IFastMultiArgObjectFacory
	{
		object CreateInstance(object[] args);
	}
}
