using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Enyim.Caching.Memcached;
using Enyim.Caching.Memcached.Results;
using Enyim.Caching.Memcached.Results.Factories;

namespace Enyim.Caching
{
    public class NullMemcachedClient : IMemcachedClient
    {
        public event Action<IMemcachedNode> NodeFailed;

        public bool Append(string key, ArraySegment<byte> data)
        {
            return true;
        }

        public CasResult<bool> Append(string key, ulong cas, ArraySegment<byte> data)
        {
            return new CasResult<bool>();
        }

        public CasResult<bool> Cas(StoreMode mode, string key, object value)
        {
            return new CasResult<bool>();
        }

        public CasResult<bool> Cas(StoreMode mode, string key, object value, ulong cas)
        {
            return new CasResult<bool>();
        }

        public CasResult<bool> Cas(StoreMode mode, string key, object value, TimeSpan validFor, ulong cas)
        {
            return new CasResult<bool>();
        }

        public CasResult<bool> Cas(StoreMode mode, string key, object value, DateTime expiresAt, ulong cas)
        {
            throw new NotImplementedException();
        }

        public ulong Decrement(string key, ulong defaultValue, ulong delta)
        {
            throw new NotImplementedException();
        }

        public ulong Decrement(string key, ulong defaultValue, ulong delta, TimeSpan validFor)
        {
            throw new NotImplementedException();
        }

        public CasResult<ulong> Decrement(string key, ulong defaultValue, ulong delta, ulong cas)
        {
            throw new NotImplementedException();
        }

        public ulong Decrement(string key, ulong defaultValue, ulong delta, DateTime expiresAt)
        {
            throw new NotImplementedException();
        }

        public CasResult<ulong> Decrement(string key, ulong defaultValue, ulong delta, TimeSpan validFor, ulong cas)
        {
            throw new NotImplementedException();
        }

        public CasResult<ulong> Decrement(string key, ulong defaultValue, ulong delta, DateTime expiresAt, ulong cas)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            
        }

        public void FlushAll()
        {
            throw new NotImplementedException();
        }

        public IDictionary<string, object> Get(IEnumerable<string> keys)
        {
            throw new NotImplementedException();
        }

        public object Get(string key)
        {
            throw new NotImplementedException();
        }

        public T Get<T>(string key)
        {
            return default(T);
        }

        public async Task<IGetOperationResult<T>> GetAsync<T>(string key)
        {
            var result = new DefaultGetOperationResultFactory<T>().Create();
            result.Success = false;
            result.Value = default(T);
            return result;
        }

        public IDictionary<string, CasResult<object>> GetWithCas(IEnumerable<string> keys)
        {
            throw new NotImplementedException();
        }

        public CasResult<object> GetWithCas(string key)
        {
            throw new NotImplementedException();
        }

        public CasResult<T> GetWithCas<T>(string key)
        {
            throw new NotImplementedException();
        }

        public ulong Increment(string key, ulong defaultValue, ulong delta)
        {
            throw new NotImplementedException();
        }

        public ulong Increment(string key, ulong defaultValue, ulong delta, TimeSpan validFor)
        {
            throw new NotImplementedException();
        }

        public CasResult<ulong> Increment(string key, ulong defaultValue, ulong delta, ulong cas)
        {
            throw new NotImplementedException();
        }

        public ulong Increment(string key, ulong defaultValue, ulong delta, DateTime expiresAt)
        {
            throw new NotImplementedException();
        }

        public CasResult<ulong> Increment(string key, ulong defaultValue, ulong delta, TimeSpan validFor, ulong cas)
        {
            throw new NotImplementedException();
        }

        public CasResult<ulong> Increment(string key, ulong defaultValue, ulong delta, DateTime expiresAt, ulong cas)
        {
            throw new NotImplementedException();
        }

        public bool Prepend(string key, ArraySegment<byte> data)
        {
            throw new NotImplementedException();
        }

        public CasResult<bool> Prepend(string key, ulong cas, ArraySegment<byte> data)
        {
            throw new NotImplementedException();
        }

        public bool Remove(string key)
        {
            return true;
        }

        public async Task<bool> RemoveAsync(string key)
        {
            return true;
        }

        public ServerStats Stats()
        {
            throw new NotImplementedException();
        }

        public ServerStats Stats(string type)
        {
            throw new NotImplementedException();
        }

        public bool Store(StoreMode mode, string key, object value)
        {
            return false;
        }

        public bool Store(StoreMode mode, string key, object value, TimeSpan validFor)
        {
            return false;
        }

        public async Task<bool> StoreAsync(StoreMode mode, string key, object value, TimeSpan validFor)
        {
            return false;
        }

        public bool Store(StoreMode mode, string key, object value, DateTime expiresAt)
        {
            return false;
        }

        public bool TryGet(string key, out object value)
        {
            throw new NotImplementedException();
        }

        public bool TryGetWithCas(string key, out CasResult<object> value)
        {
            throw new NotImplementedException();
        }

        public void Add(string key, object value, int cacheSeconds)
        {
        }

        public async Task AddAsync(string key, object value, int cacheSeconds)
        {
        }
    }
}
