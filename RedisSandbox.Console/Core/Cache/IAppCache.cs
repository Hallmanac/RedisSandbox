using System;
using System.Collections.Generic;

namespace RedisSandbox.Console.Core.Cache
{
    public interface IAppCache
    {
        bool Contains(string key);
        object Get(string key, string indexName = "");
        TValue GetValue<TValue>(string key, string indexName = "") where TValue : class;
        void Remove(string key, string indexName = "");
        void Put(string key, object value, TimeSpan? timeout, string indexName = "");
        IEnumerable<TValue> GetAllIndexedItemsInCache<TValue>(string indexName) where TValue : class;
        TValue GetItemViaIndex<TValue>(string indexName, string indexValue) where TValue : class;
        void SetIndex(string indexName, KeyValuePair<string, string> keyValuePair);
        void ClearCache(); 
    }
}