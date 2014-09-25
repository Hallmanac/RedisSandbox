using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace RedisSandbox.Console.Core.Cache
{
    public class RedisCache : IAppCache
    {
        private readonly IDatabase _redisCache;

        public RedisCache() { _redisCache = ConnectionMultiplexer.Connect(AppConst.RedisConnectionString).GetDatabase(); }

        public bool Contains(string key) { return _redisCache.KeyExists(key); }

        public object Get(string key, string indexName = "")
        {
            if(string.IsNullOrEmpty(key))
                return null;
            var cacheValue = _redisCache.StringGet(key);
            if(cacheValue.IsNullOrEmpty)
            {
                if(!string.IsNullOrEmpty(indexName))
                    _redisCache.SetRemove(indexName, key);
                return null;
            }
            var objValue = JsonConvert.DeserializeObject(cacheValue);
            if(string.IsNullOrEmpty(indexName))
                return objValue;
            _redisCache.SetAdd(indexName, key);
            return objValue;
        }

        public TValue GetValue<TValue>(string key, string indexName = "") where TValue : class
        {
            if(string.IsNullOrEmpty(key))
                return null;
            var cachedValue = _redisCache.StringGet(key);
            if(cachedValue.IsNullOrEmpty)
            {
                if(!string.IsNullOrEmpty(indexName))
                    _redisCache.SetRemove(indexName, key);
                return null;
            }
            var result = JsonConvert.DeserializeObject<TValue>(cachedValue);
            if(string.IsNullOrEmpty(indexName))
                return result;
            _redisCache.SetAdd(indexName, key);
            return result;
        }

        public void Remove(string key, string indexName = "")
        {
            if(string.IsNullOrEmpty(key))
                return;
            _redisCache.KeyDelete(key);
            if(string.IsNullOrEmpty(indexName))
                return;
            _redisCache.SetRemove(indexName, key);
        }

        public void Put(string key, object value, TimeSpan? timeout, string indexName = "")
        {
            if(string.IsNullOrEmpty(key) || value == null)
                return;
            var cacheValue = JsonConvert.SerializeObject(value);
            if(timeout == null)
                _redisCache.StringSet(key, cacheValue);
            else
                _redisCache.StringSet(key, cacheValue, timeout);
            if(string.IsNullOrEmpty(indexName))
                return;
            _redisCache.SetAdd(indexName, key);
        }

        public IEnumerable<TValue> GetAllIndexedItemsInCache<TValue>(string indexName) where TValue : class
        {
            if(string.IsNullOrEmpty(indexName))
                yield break;

            //var keysToRemove = new List<RedisValue>();
            //var indexedValues = new List<TValue>();
            var previouslyScannedMembers = new List<string>();
            if(!_redisCache.KeyExists(indexName))
                yield break;
            foreach(var redisValue in _redisCache.SetScan(indexName).Where(redisValue => !previouslyScannedMembers.Contains(redisValue.ToString())))
            {
                previouslyScannedMembers.Add(redisValue);
                var cachedValue = _redisCache.StringGet(redisValue.ToString());

                // If the item is null it means we have a stale key in the index and need to remove it (typically from when a cache item naturally expires). 
                // We will add it to the "keysToRemove" list to go through after this foreach statement.
                if(string.IsNullOrEmpty(cachedValue))
                    _redisCache.SetRemove(indexName, redisValue); //keysToRemove.Add(redisValue);
                else // We simply add the cacheItem to the list of indexedValues that we will eventually return.
                {
                    var cachedItem = JsonConvert.DeserializeObject<TValue>(cachedValue);
                    yield return cachedItem;
                }
            }

            // Remove any keys that are stale
            /*if (keysToRemove.Count <= 0)
            {
                return indexedValues;
            }
            _redisCache.SetRemove(indexName, keysToRemove.ToArray());
            return indexedValues;*/
        }

        public TValue GetItemViaIndex<TValue>(string indexName, string indexValue) where TValue : class
        {
            var cacheKey = _redisCache.HashGet(indexName, indexValue);
            return cacheKey.IsNullOrEmpty ? default(TValue) : GetValue<TValue>(cacheKey);
        }

        public void ClearCache()
        {
            var endpoints = ConnectionMultiplexer.Connect(AppConst.RedisConnectionString).GetEndPoints();
            foreach(var server in endpoints.Select(endpoint => ConnectionMultiplexer.Connect(AppConst.RedisConnectionString).GetServer(endpoint)))
            {
                server.FlushAllDatabases();
            }
        }

        public void SetIndex(string indexName, KeyValuePair<string, string> keyValuePair)
        {
            _redisCache.HashSet(indexName, keyValuePair.Key, keyValuePair.Value);
        }
    }
}