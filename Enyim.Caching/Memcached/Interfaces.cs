using System;

namespace Enyim.Caching.Memcached
{
	internal interface IOperation
	{
		bool Execute();
	}

	internal interface IGetOperation : IOperation
	{
		object Result { get; }
	}

	internal interface IMutatorOperation : IOperation
	{
		ulong Result { get; }
	}

	internal interface IProtocolImplementation : IDisposable
	{
		object Get(string key);
		bool TryGet(string key, out object value);

		bool Store(StoreMode mode, string key, object value, uint expiration);
		bool Delete(string key);
		ulong Mutate(MutationMode mode, string key, ulong startValue, ulong step, uint expiration);
	}

	public enum MutationMode { Increment, Decrement };
}
