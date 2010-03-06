
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
