using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace RedisSandbox.Console.Core.Cache
{
    public class RedisCache : IAppCache
    {
        private readonly IDatabase _redisCache;

        public RedisCache()
        {
            // We call a static singleton property to ensure that we don't create multiple connections to the multiplexer.
            // The multiplexer handles the balancing of connection pools.
            // This is particularly valueable in ASP.NET applications where you might new up several instances of this cache provider for a given request. And
            // since we're not destroying those connections we end up maintaining hundreds upon thousands of connections which starves memory and resources.
            _redisCache = AppConst.RedisMultiplexer.GetDatabase();
        }

        public bool Contains(string key) { return _redisCache.KeyExists(key); }

        public async Task<bool> ContainsAsync(string key) { return await _redisCache.KeyExistsAsync(key).ConfigureAwait(false); }

        public object Get(string key, string trackingIndexName = "")
        {
            if(string.IsNullOrEmpty(key))
                return null;
            var cacheValue = _redisCache.StringGet(key);

            // If we have a null value, be sure to remove it from the tracking index
            if(cacheValue.IsNullOrEmpty)
            {
                if(!string.IsNullOrEmpty(trackingIndexName))
                    _redisCache.SetRemove(ComposeTrackingIndexKey(trackingIndexName), key);
                return null;
            } // else...
            var objValue = JsonConvert.DeserializeObject(cacheValue);
            if(string.IsNullOrEmpty(trackingIndexName))
                return objValue;

            // Add it to the tracking index
            _redisCache.SetAdd(ComposeTrackingIndexKey(trackingIndexName), key);
            return objValue;
        }

        public async Task<object> GetAsync(string key, string trackingIndexName = "")
        {
            if(string.IsNullOrEmpty(key))
                return null;
            var cacheValue = await _redisCache.StringGetAsync(key).ConfigureAwait(false);

            // If we have a null value, be sure to remove it from the tracking index
            if(cacheValue.IsNullOrEmpty)
            {
                if(!string.IsNullOrEmpty(trackingIndexName))
                    await _redisCache.SetRemoveAsync(ComposeTrackingIndexKey(trackingIndexName), key).ConfigureAwait(false);
                return null;
            } // else...
            var objValue = JsonConvert.DeserializeObject(cacheValue);
            if(string.IsNullOrEmpty(trackingIndexName))
                return objValue;

            // Add it to the tracking index
            await _redisCache.SetAddAsync(ComposeTrackingIndexKey(trackingIndexName), key).ConfigureAwait(false);
            return objValue;
        }

        public TValue GetValue<TValue>(string key, string trackingIndexName = "") where TValue : class
        {
            if(string.IsNullOrEmpty(key))
                return null;
            var cachedValue = _redisCache.StringGet(key);

            // If we have a null value, be sure to remove it from the tracking index
            if(cachedValue.IsNullOrEmpty)
            {
                if(!string.IsNullOrEmpty(trackingIndexName))
                    _redisCache.SetRemove(ComposeTrackingIndexKey(trackingIndexName), key);
                return null;
            } // else...
            var result = JsonConvert.DeserializeObject<TValue>(cachedValue);
            if(string.IsNullOrEmpty(trackingIndexName))
                return result;

            // Add it to the tracking index
            _redisCache.SetAdd(ComposeTrackingIndexKey(trackingIndexName), key);
            return result;
        }

        public async Task<TValue> GetValueAsync<TValue>(string key, string trackingIndexName = "") where TValue : class
        {
            if(string.IsNullOrEmpty(key))
                return null;
            var cachedValue = await _redisCache.StringGetAsync(key).ConfigureAwait(false);

            // If we have a null value, be sure to remove it from the tracking index
            if(cachedValue.IsNullOrEmpty)
            {
                if(!string.IsNullOrEmpty(trackingIndexName))
                    await _redisCache.SetRemoveAsync(ComposeTrackingIndexKey(trackingIndexName), key).ConfigureAwait(false);
                return null;
            } // else...
            var result = JsonConvert.DeserializeObject<TValue>(cachedValue);
            if(string.IsNullOrEmpty(trackingIndexName))
                return result;

            // Add it to the tracking index
            await _redisCache.SetAddAsync(ComposeTrackingIndexKey(trackingIndexName), key).ConfigureAwait(false);
            return result;
        }

        public void Remove(string key, string trackingIndexName = "")
        {
            if(string.IsNullOrEmpty(key))
                return;

            // Remove the object from cache
            _redisCache.KeyDelete(key);
            if(string.IsNullOrEmpty(trackingIndexName))
                return;

            // Remove it from the tracking index
            _redisCache.SetRemove(ComposeTrackingIndexKey(trackingIndexName), key);
        }

        public async Task RemoveAsync(string key, string trackingIndexName = "")
        {
            if(string.IsNullOrEmpty(key))
                return;

            // Remove the object from cache
            await _redisCache.KeyDeleteAsync(key).ConfigureAwait(false);
            if(string.IsNullOrEmpty(trackingIndexName))
                return;

            // Remove it from the tracking index
            await _redisCache.SetRemoveAsync(ComposeTrackingIndexKey(trackingIndexName), key).ConfigureAwait(false);
        }

        public void AddOrUpdate(string key, object value, TimeSpan? timeout, string trackingIndexName = "")
        {
            if(string.IsNullOrEmpty(key) || value == null)
                return;
            var cacheValue = JsonConvert.SerializeObject(value);
            if(timeout == null)
                _redisCache.StringSet(key, cacheValue);
            else
                _redisCache.StringSet(key, cacheValue, timeout);
            if(string.IsNullOrEmpty(trackingIndexName))
                return;
            _redisCache.SetAdd(ComposeTrackingIndexKey(trackingIndexName), key);
        }

        public async Task AddOrUpdateAsync(string key, object value, TimeSpan? timeout, string trackingIndexName = "")
        {
            if(string.IsNullOrEmpty(key) || value == null)
                return;
            var cacheValue = JsonConvert.SerializeObject(value);
            if(timeout == null)
                await _redisCache.StringSetAsync(key, cacheValue).ConfigureAwait(false);
            else
                await _redisCache.StringSetAsync(key, cacheValue, timeout).ConfigureAwait(false);
            if(string.IsNullOrEmpty(trackingIndexName))
                return;
            await _redisCache.SetAddAsync(ComposeTrackingIndexKey(trackingIndexName), key).ConfigureAwait(false);
        }

        public List<TValue> GetAllTrackedItemsInCache<TValue>(string trackingIndexName) where TValue : class
        {
            return GetAllTrackedItemsInCacheAsync<TValue>(trackingIndexName).Result;
        }

        public async Task<List<TValue>> GetAllTrackedItemsInCacheAsync<TValue>(string trackingIndexName) where TValue : class
        {
            if(string.IsNullOrEmpty(trackingIndexName))
                return new List<TValue>();
            if(!_redisCache.KeyExists(ComposeTrackingIndexKey(trackingIndexName)))
                return new List<TValue>();
            var setMembers = await _redisCache.SetMembersAsync(ComposeTrackingIndexKey(trackingIndexName)).ConfigureAwait(false);
            if(setMembers.Length < 1)
                return new List<TValue>();
            var resultList = new List<TValue>();
            foreach(var member in setMembers)
            {
                var cachedItem = await GetValueAsync<TValue>(member.ToString()).ConfigureAwait(false);
                if(cachedItem == default(TValue))
                {
                    await _redisCache.SetRemoveAsync(ComposeTrackingIndexKey(trackingIndexName), member).ConfigureAwait(false);
                    continue;
                }
                resultList.Add(cachedItem);
            }
            return resultList;
        }

        public TValue GetItemFromIndex<TValue>(string indexName, string hashKey) where TValue : class
        {
            var cacheKey = _redisCache.HashGet(ComposeCustomIndexKey(indexName), hashKey);
            if(cacheKey.IsNullOrEmpty)
                return default(TValue);

            // Get the value from cache
            var cacheValue = GetValue<TValue>(cacheKey);

            // If we have a null value then we need to remove it from the hashset index
            if(cacheValue == default(TValue))
                RemoveFromCustomIndex(ComposeCustomIndexKey(indexName), hashKey);
            return cacheValue;
        }

        public async Task<TValue> GetItemFromIndexAsync<TValue>(string indexName, string hashKey) where TValue : class
        {
            var cacheKey = await _redisCache.HashGetAsync(ComposeCustomIndexKey(indexName), hashKey).ConfigureAwait(false);
            if(cacheKey.IsNullOrEmpty)
                return default(TValue);

            // Get the value from cache
            var cacheValue = await GetValueAsync<TValue>(cacheKey).ConfigureAwait(false);

            // If we have a null value then we need to remove it from the hashset index
            if(cacheValue == default(TValue))
                await RemoveFromCustomIndexAsync(ComposeCustomIndexKey(indexName), hashKey).ConfigureAwait(false);
            return cacheValue;
        }

        public List<TValue> GetAllItemsFromIndex<TValue>(string indexName) where TValue : class
        {
            return GetAllItemsFromIndexAsync<TValue>(indexName).Result;
        }

        public async Task<List<TValue>> GetAllItemsFromIndexAsync<TValue>(string indexName) where TValue : class
        {
            if(string.IsNullOrEmpty(indexName))
                return new List<TValue>();
            if(!_redisCache.KeyExists(ComposeCustomIndexKey(indexName)))
                return new List<TValue>();
            // Get all the hash entries at the specified cache key
            var hashSetAtIndex = await _redisCache.HashGetAllAsync(ComposeCustomIndexKey(indexName)).ConfigureAwait(false);
            if(hashSetAtIndex.Length < 1)
                return new List<TValue>();
            
            var resultList = new List<TValue>();

            foreach(var hashEntry in hashSetAtIndex)
            {
                var hashKey = hashEntry.Name.ToString();
                var hashVal = hashEntry.Value.ToString();

                // Get the cached item value
                var cachedItem = await GetValueAsync<TValue>(hashVal).ConfigureAwait(false);

                // If we have a null then we remove it from the custom index and continue the iteration
                if(cachedItem == default(TValue))
                {
                    await RemoveFromCustomIndexAsync(indexName, hashKey).ConfigureAwait(false);
                    continue;
                }
                // Else we add it to the result list
                resultList.Add(cachedItem);
            }
            return resultList;
        }

        public void RemoveFromCustomIndex(string indexName, string hashKey) { _redisCache.HashDelete(ComposeCustomIndexKey(indexName), hashKey); }

        public async Task RemoveFromCustomIndexAsync(string indexName, string hashKey)
        {
            await _redisCache.HashDeleteAsync(ComposeCustomIndexKey(indexName), hashKey).ConfigureAwait(false);
        }

        public void SetItemForCustomIndex(string indexName, KeyValuePair<string, string> hashSet)
        {
            _redisCache.HashSet(ComposeCustomIndexKey(indexName), hashSet.Key, hashSet.Value);
        }

        public async Task SetItemForCustomIndexAsync(string indexName, KeyValuePair<string, string> hashSet)
        {
            await _redisCache.HashSetAsync(ComposeCustomIndexKey(indexName), hashSet.Key, hashSet.Value).ConfigureAwait(false);
        }

        public void ClearCache()
        {
            var endpoints = ConnectionMultiplexer.Connect(AppConst.RedisConnectionString).GetEndPoints();
            foreach(var server in endpoints.Select(endpoint => ConnectionMultiplexer.Connect(AppConst.RedisConnectionString).GetServer(endpoint)))
            {
                server.FlushAllDatabases();
            }
        }

        public async Task ClearCacheAsync()
        {
            var connection = await ConnectionMultiplexer.ConnectAsync(AppConst.RedisConnectionString).ConfigureAwait(false);
            var endpoints = connection.GetEndPoints();
            foreach(var endPoint in endpoints)
            {
                var serverConnect = await ConnectionMultiplexer.ConnectAsync(AppConst.RedisConnectionString).ConfigureAwait(false);
                var server = serverConnect.GetServer(endPoint);
                await server.FlushAllDatabasesAsync().ConfigureAwait(false);
            }
        }

        private static string ComposeTrackingIndexKey(string indexName) { return string.Format("{0}_{1}", "Tracking", indexName); }

        private static string ComposeCustomIndexKey(string indexName) { return string.Format("{0}_{1}", "Custom", indexName); }
    }
}