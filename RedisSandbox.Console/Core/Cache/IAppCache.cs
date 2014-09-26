using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RedisSandbox.Console.Core.Cache
{
    public interface IAppCache
    {
        bool Contains(string key);
        Task<bool> ContainsAsync(string key);
        object Get(string key, string trackingIndexName = "");
        Task<object> GetAsync(string key, string trackingIndexName = "");
        TValue GetValue<TValue>(string key, string trackingIndexName = "") where TValue : class;
        Task<TValue> GetValueAsync<TValue>(string key, string trackingIndexName = "") where TValue : class;
        void Remove(string key, string trackingIndexName = "");
        Task RemoveAsync(string key, string trackingIndexName = "");
        void Put(string key, object value, TimeSpan? timeout, string trackingIndexName = "");
        Task PutAsync(string key, object value, TimeSpan? timeout, string trackingIndexName = "");
        TValue GetItemViaIndex<TValue>(string indexName, string hashKey) where TValue : class;
        Task<TValue> GetItemViaIndexAsync<TValue>(string indexName, string hashKey) where TValue : class;
        void SetCustomIndex(string indexName, KeyValuePair<string, string> hashSet);
        Task SetCustomIndexAsync(string indexName, KeyValuePair<string, string> hashSet);
        void RemoveFromCustomIndex(string indexName, string hashKey);
        Task RemoveFromCustomIndexAsync(string indexName, string hashKey);
        IEnumerable<TValue> GetAllTrackedItemsInCache<TValue>(string trackingIndexName) where TValue : class;
        void ClearCache();
        Task ClearCacheAsync();
    }
}