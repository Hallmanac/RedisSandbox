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

        public RedisCache() { _redisCache = ConnectionMultiplexer.Connect(AppConst.RedisConnectionString).GetDatabase(); }

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
                    _redisCache.SetRemove(trackingIndexName, key);
                return null;
            } // else...
            var objValue = JsonConvert.DeserializeObject(cacheValue);
            if(string.IsNullOrEmpty(trackingIndexName))
                return objValue;

            // Add it to the tracking index
            _redisCache.SetAdd(trackingIndexName, key);
            return objValue;
        }

        public async Task<object> GetAsync(string key, string trackingIndexName = "")
        {
            if (string.IsNullOrEmpty(key))
                return null;
            var cacheValue = await _redisCache.StringGetAsync(key).ConfigureAwait(false);

            // If we have a null value, be sure to remove it from the tracking index
            if (cacheValue.IsNullOrEmpty)
            {
                if(!string.IsNullOrEmpty(trackingIndexName))
                    await _redisCache.SetRemoveAsync(trackingIndexName, key).ConfigureAwait(false);
                return null;
            } // else...
            var objValue = JsonConvert.DeserializeObject(cacheValue);
            if (string.IsNullOrEmpty(trackingIndexName))
                return objValue;

            // Add it to the tracking index
            await _redisCache.SetAddAsync(trackingIndexName, key).ConfigureAwait(false);
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
                    _redisCache.SetRemove(trackingIndexName, key);
                return null;
            } // else...
            var result = JsonConvert.DeserializeObject<TValue>(cachedValue);
            if(string.IsNullOrEmpty(trackingIndexName))
                return result;

            // Add it to the tracking index
            _redisCache.SetAdd(trackingIndexName, key);
            return result;
        }

        public async Task<TValue> GetValueAsync<TValue>(string key, string trackingIndexName = "") where TValue : class
        {
            if (string.IsNullOrEmpty(key))
                return null;
            var cachedValue = await _redisCache.StringGetAsync(key).ConfigureAwait(false);

            // If we have a null value, be sure to remove it from the tracking index
            if (cachedValue.IsNullOrEmpty)
            {
                if (!string.IsNullOrEmpty(trackingIndexName))
                    await _redisCache.SetRemoveAsync(trackingIndexName, key).ConfigureAwait(false);
                return null;
            } // else...
            var result = JsonConvert.DeserializeObject<TValue>(cachedValue);
            if (string.IsNullOrEmpty(trackingIndexName))
                return result;

            // Add it to the tracking index
            await _redisCache.SetAddAsync(trackingIndexName, key).ConfigureAwait(false);
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
            _redisCache.SetRemove(trackingIndexName, key);
        }

        public async Task RemoveAsync(string key, string trackingIndexName = "")
        {
            if (string.IsNullOrEmpty(key))
                return;

            // Remove the object from cache
            await _redisCache.KeyDeleteAsync(key).ConfigureAwait(false);
            if (string.IsNullOrEmpty(trackingIndexName))
                return;

            // Remove it from the tracking index
            await _redisCache.SetRemoveAsync(trackingIndexName, key).ConfigureAwait(false);
        }

        public void Put(string key, object value, TimeSpan? timeout, string trackingIndexName = "")
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
            _redisCache.SetAdd(trackingIndexName, key);
        }

        public async Task PutAsync(string key, object value, TimeSpan? timeout, string trackingIndexName = "")
        {
            if (string.IsNullOrEmpty(key) || value == null)
                return;
            var cacheValue = JsonConvert.SerializeObject(value);
            if(timeout == null)
                await _redisCache.StringSetAsync(key, cacheValue).ConfigureAwait(false);
            else
                await _redisCache.StringSetAsync(key, cacheValue, timeout).ConfigureAwait(false);
            if (string.IsNullOrEmpty(trackingIndexName))
                return;
            await _redisCache.SetAddAsync(trackingIndexName, key).ConfigureAwait(false);
        }

        public IEnumerable<TValue> GetAllTrackedItemsInCache<TValue>(string trackingIndexName) where TValue : class
        {
            if(string.IsNullOrEmpty(trackingIndexName))
                yield break;

            var previouslyScannedMembers = new List<string>();
            if(!_redisCache.KeyExists(trackingIndexName))
                yield break;
            foreach(var redisValue in _redisCache.SetScan(trackingIndexName).Where(redisValue => !previouslyScannedMembers.Contains(redisValue.ToString())))
            {
                previouslyScannedMembers.Add(redisValue);
                var cachedValue = _redisCache.StringGet(redisValue.ToString());

                if(string.IsNullOrEmpty(cachedValue))
                    _redisCache.SetRemove(trackingIndexName, redisValue); //keysToRemove.Add(redisValue);
                else // We simply add the cacheItem to the list of indexedValues that we will eventually return.
                {
                    var cachedItem = JsonConvert.DeserializeObject<TValue>(cachedValue);
                    yield return cachedItem;
                }
            }
        }

        public TValue GetItemViaIndex<TValue>(string indexName, string hashKey) where TValue : class
        {
            var cacheKey = _redisCache.HashGet(indexName, hashKey);
            if(cacheKey.IsNullOrEmpty)
                return default(TValue);

            // Get the value from cache
            var cacheValue = GetValue<TValue>(cacheKey);

            // If we have a null value then we need to remove it from the hashset index
            if(cacheValue == default(TValue))
                RemoveFromCustomIndex(indexName, hashKey);
            return cacheValue;
        }

        public async Task<TValue> GetItemViaIndexAsync<TValue>(string indexName, string hashKey) where TValue : class
        {
            var cacheKey = await _redisCache.HashGetAsync(indexName, hashKey).ConfigureAwait(false);
            if (cacheKey.IsNullOrEmpty)
                return default(TValue);

            // Get the value from cache
            var cacheValue = await GetValueAsync<TValue>(cacheKey).ConfigureAwait(false);

            // If we have a null value then we need to remove it from the hashset index
            if (cacheValue == default(TValue))
                await RemoveFromCustomIndexAsync(indexName, hashKey).ConfigureAwait(false);
            return cacheValue;
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

        public void RemoveFromCustomIndex(string indexName, string hashKey)
        {
            _redisCache.HashDelete(indexName, hashKey);
        }

        public async Task RemoveFromCustomIndexAsync(string indexName, string hashKey)
        {
            await _redisCache.HashDeleteAsync(indexName, hashKey).ConfigureAwait(false);
        }

        public void SetCustomIndex(string indexName, KeyValuePair<string, string> hashSet)
        {
            _redisCache.HashSet(indexName, hashSet.Key, hashSet.Value);
        }

        public async Task SetCustomIndexAsync(string indexName, KeyValuePair<string, string> hashSet)
        {
            await _redisCache.HashSetAsync(indexName, hashSet.Key, hashSet.Value).ConfigureAwait(false);
        }
    }
}