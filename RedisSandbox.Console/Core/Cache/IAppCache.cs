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

        void AddOrUpdate(string key, object value, TimeSpan? timeout, string trackingIndexName = "");
        Task AddOrUpdateAsync(string key, object value, TimeSpan? timeout, string trackingIndexName = "");

        TValue GetItemFromIndex<TValue>(string indexName, string hashKey) where TValue : class;
        Task<TValue> GetItemFromIndexAsync<TValue>(string indexName, string hashKey) where TValue : class;

        List<TValue> GetAllItemsFromIndex<TValue>(string indexName) where TValue : class;
        Task<List<TValue>> GetAllItemsFromIndexAsync<TValue>(string indexName) where TValue : class; 

        void SetItemForCustomIndex(string indexName, KeyValuePair<string, string> hashSet);
        Task SetItemForCustomIndexAsync(string indexName, KeyValuePair<string, string> hashSet);

        void RemoveFromCustomIndex(string indexName, string hashKey);
        Task RemoveFromCustomIndexAsync(string indexName, string hashKey);

        List<TValue> GetAllTrackedItemsInCache<TValue>(string trackingIndexName) where TValue : class;
        Task<List<TValue>> GetAllTrackedItemsInCacheAsync<TValue>(string trackingIndexName) where TValue : class; 

        void ClearCache();
        Task ClearCacheAsync();
    }
}