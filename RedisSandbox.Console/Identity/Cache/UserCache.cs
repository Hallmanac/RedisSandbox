using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RedisSandbox.Console.Core.Cache;

namespace RedisSandbox.Console.Identity.Cache
{
    public class UserCache
    {
        private readonly IAppCache _appCache;

        public UserCache() { _appCache = _appCache ?? new RedisCache(); }

        public UserCache(IAppCache appCache) : this() { _appCache = appCache; }

        public User GetById(int id)
        {
            // The ID index is stored as a hash set or Concurrent Dictionary so we call into that index in cache
            // and it will give the proper cache key that will allow us to retrieve the full user from cache.
            var theUser = _appCache.GetItemFromIndex<User>(ComposeUserIdIndexKey(), id.ToString());
            return theUser;
        }

        public async Task<User> GetByIdAsync(int id)
        {
            // The ID index is stored as a hash set or Concurrent Dictionary so we call into that index in cache
            // and it will give the proper cache key that will allow us to retrieve the full user from cache.
            var theUser = await _appCache.GetItemFromIndexAsync<User>(ComposeUserIdIndexKey(), id.ToString()).ConfigureAwait(false);
            return theUser;
        }

        public User GetByUserName(string userName)
        {
            // Username is the part of the default key for caching a user so we can just get it straight from cache directly
            return _appCache.GetValue<User>(ComposeKey(userName), ComposeIndexKey());
        }

        public async Task<User> GetByUserNameAsync(string userName)
        {
            // Username is the part of the default key for caching a user so we can just get it straight from cache directly
            return await _appCache.GetValueAsync<User>(ComposeKey(userName), ComposeIndexKey()).ConfigureAwait(false);
        }

        public void PutUserInCache(User user)
        {
            // Put the object into the cache
            _appCache.AddOrUpdate(ComposeKey(user.Username), user, TimeSpan.FromDays(30), ComposeIndexKey());

            // Set the index for Emails
            user.Emails.ForEach(eml => _appCache.SetItemForCustomIndex(ComposeEmailIndexKey(), new KeyValuePair<string, string>(eml.EmailAddress, ComposeKey(user.Username))));

            // Set the index for user id
            _appCache.SetItemForCustomIndex(ComposeUserIdIndexKey(), new KeyValuePair<string, string>(user.Id.ToString(), ComposeKey(user.Username)));
        }

        public async Task PutUserInCacheAsync(User user)
        {
            // Put the object into the cache
            await _appCache.AddOrUpdateAsync(ComposeKey(user.Username), user, TimeSpan.FromDays(30), ComposeIndexKey()).ConfigureAwait(false);

            // Set the index for Emails
            user.Emails.ForEach(
                async eml =>
                    await
                        _appCache.SetItemForCustomIndexAsync(ComposeEmailIndexKey(), new KeyValuePair<string, string>(eml.EmailAddress, ComposeKey(user.Username)))
                                 .ConfigureAwait(false));

            // Set the index for user id
            await
                _appCache.SetItemForCustomIndexAsync(ComposeUserIdIndexKey(), new KeyValuePair<string, string>(user.Id.ToString(), ComposeKey(user.Username)))
                         .ConfigureAwait(false);
        }

        public void RemoveUserFromCache(User user)
        {
            _appCache.Remove(ComposeKey(user.Username), ComposeIndexKey());
            user.Emails.ForEach(eml => _appCache.RemoveFromCustomIndex(ComposeEmailIndexKey(), eml.EmailAddress));
            _appCache.RemoveFromCustomIndex(ComposeUserIdIndexKey(), user.Id.ToString());
        }

        public async Task RemoveUserFromCacheAsync(User user)
        {
            await _appCache.RemoveAsync(ComposeKey(user.Username), ComposeIndexKey()).ConfigureAwait(false);
            user.Emails.ForEach(async eml => await _appCache.RemoveFromCustomIndexAsync(ComposeEmailIndexKey(), eml.EmailAddress).ConfigureAwait(false));
            await _appCache.RemoveFromCustomIndexAsync(ComposeUserIdIndexKey(), user.Id.ToString()).ConfigureAwait(false);
        }

        public IEnumerable<User> GetAllUsersInCache()
        {
            return _appCache.GetAllTrackedItemsInCache<User>(ComposeIndexKey());
        }

        public async Task<List<User>> GetAllUsersInCacheAsync()
        {
            return await _appCache.GetAllTrackedItemsInCacheAsync<User>(ComposeIndexKey()).ConfigureAwait(false);
        }  

        public User GetByEmail(string emailAddress)
        {
            var theUser = _appCache.GetItemFromIndex<User>(ComposeEmailIndexKey(), emailAddress);
            return theUser;
        }

        public async Task<User> GetByEmailAsync(string emailAddress)
        {
            var theUser = await _appCache.GetItemFromIndexAsync<User>(ComposeEmailIndexKey(), emailAddress).ConfigureAwait(false);
            return theUser;
        }

        public void ClearCache() { _appCache.ClearCache(); }

        #region Key Composers

        private static string ComposeKey(string keyItem) { return string.Format("{0}_{1}", "UserEntity", keyItem); }

        private static string ComposeIndexKey() { return string.Format("{0}", "UsersIndexList"); }

        private static string ComposeEmailIndexKey() { return string.Format("{0}:{1}", "User", "Emails"); }

        private static string ComposeUserIdIndexKey() { return string.Format("{0}:{1}", "User", "Ids"); }

        #endregion

        public async Task ClearCacheAsync() { await _appCache.ClearCacheAsync(); }
    }
}