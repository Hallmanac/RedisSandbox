using System;
using System.Collections.Generic;
using System.Linq;
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

        public void AddOrUpdateUser(User user)
        {
            // Put the object into the cache
            _appCache.AddOrUpdate(ComposeKey(user.Username), user, TimeSpan.FromDays(30), ComposeIndexKey());

            // Set the index for Emails
            user.Emails.ForEach(
                eml => _appCache.SetItemForCustomIndex(ComposeEmailIndexKey(), new KeyValuePair<string, string>(eml.EmailAddress, ComposeKey(user.Username))));

            // Set the index for user id
            _appCache.SetItemForCustomIndex(ComposeUserIdIndexKey(), new KeyValuePair<string, string>(user.Id.ToString(), ComposeKey(user.Username)));

            // Set index for user group
            _appCache.SetItemForCustomIndex(ComposeUserGroupIndexKey(user.UserGroup), new KeyValuePair<string, string>(user.Username, user.Username));
        }

        public async Task PutUserInCacheAsync(User user)
        {
            // Put the object into the cache
            await _appCache.AddOrUpdateAsync(ComposeKey(user.Username), user, TimeSpan.FromDays(30), ComposeIndexKey()).ConfigureAwait(false);

            // Set the index for Emails
            user.Emails.ForEach(
                async eml =>
                    await
                        _appCache.SetItemForCustomIndexAsync(ComposeEmailIndexKey(),
                            new KeyValuePair<string, string>(eml.EmailAddress, ComposeKey(user.Username)))
                                 .ConfigureAwait(false));

            // Set the index for user id
            await
                _appCache.SetItemForCustomIndexAsync(ComposeUserIdIndexKey(), new KeyValuePair<string, string>(user.Id.ToString(), ComposeKey(user.Username)))
                         .ConfigureAwait(false);

            // Put the user in the user group index based on their user group
            await
                _appCache.SetItemForCustomIndexAsync(ComposeUserGroupIndexKey(user.UserGroup),
                    new KeyValuePair<string, string>(user.Username, ComposeKey(user.Username)))
                         .ConfigureAwait(false);
            await _appCache.AddOrUpdateAsync(ComposeUserGroupKey(user.UserGroup), user.UserGroup, null, ListOfUserGroupsKey).ConfigureAwait(false);
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

        public IEnumerable<User> GetAllUsersInCache() { return _appCache.GetAllTrackedItemsInCache<User>(ComposeIndexKey()); }

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

        public async Task<List<User>> GetAllUsersInGroupAsync(string groupName)
        {
            return await _appCache.GetAllItemsFromIndexAsync<User>(ComposeUserGroupIndexKey(groupName)).ConfigureAwait(false);
        }

        public async Task<List<string>> GetAllUserGroupNamesAsync()
        {
            var resultList = await _appCache.GetAllTrackedItemsInCacheAsync<string>(ListOfUserGroupsKey).ConfigureAwait(false);
            return resultList.OrderBy(name => name).ToList();
        }

        public void ClearCache() { _appCache.ClearCache(); }

        public async Task ClearCacheAsync() { await _appCache.ClearCacheAsync(); }

        #region Key Composers
        private const string ListOfUserGroupsKey = "ListOfUserGroupsKey";
        private static string ComposeKey(string keyItem) { return string.Format("{0}_{1}", "UserEntity", keyItem); }

        private static string ComposeIndexKey() { return string.Format("{0}", "UsersIndexList"); }

        private static string ComposeEmailIndexKey() { return string.Format("{0}:{1}", "User", "Emails"); }

        private static string ComposeUserIdIndexKey() { return string.Format("{0}:{1}", "User", "Ids"); }

        private static string ComposeUserGroupIndexKey(string groupName) { return string.Format("UserGroup:{0}", groupName); }

        private static string ComposeUserGroupKey(string userGroup) { return string.Format("UserGroup_{0}", userGroup); }
        #endregion
    }
}