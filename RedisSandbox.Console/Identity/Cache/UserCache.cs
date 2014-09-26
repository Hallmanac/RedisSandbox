using System;
using System.Collections.Generic;
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
            var theUser = _appCache.GetItemViaIndex<User>(ComposeUserIdIndexKey(), id.ToString());
            return theUser;
        }

        public User GetByUserName(string userName)
        {
            // Username is the part of the default key for caching a user so we can just get it straight from cache directly
            return _appCache.GetValue<User>(ComposeKey(userName), ComposeIndexKey());
        }

        public void PutUserInCache(User user)
        {
            // Put the object into the cache
            _appCache.Put(ComposeKey(user.Username), user, TimeSpan.FromDays(30), ComposeIndexKey());

            // Set the index for Emails
            user.Emails.ForEach(eml => _appCache.SetCustomIndex(ComposeEmailIndexKey(), new KeyValuePair<string, string>(eml.EmailAddress, ComposeKey(user.Username))));

            // Set the index for user id
            _appCache.SetCustomIndex(ComposeUserIdIndexKey(), new KeyValuePair<string, string>(user.Id.ToString(), ComposeKey(user.Username)));
        }

        public void RemoveUserFromCache(User user)
        {
            _appCache.Remove(ComposeKey(user.Username), ComposeIndexKey());
            user.Emails.ForEach(eml => _appCache.RemoveFromCustomIndex(ComposeEmailIndexKey(), eml.EmailAddress));
            _appCache.RemoveFromCustomIndex(ComposeUserIdIndexKey(), user.Id.ToString());
        }

        public IEnumerable<User> GetAllUsersInCache()
        {
            return _appCache.GetAllTrackedItemsInCache<User>(ComposeIndexKey());
        }

        public User GetByEmail(string emailAddress)
        {
            var theUser = _appCache.GetItemViaIndex<User>(ComposeEmailIndexKey(), emailAddress);
            return theUser;
        }

        public void ClearCache() { _appCache.ClearCache(); }

        #region Key Composers

        private static string ComposeKey(string keyItem) { return string.Format("{0}_{1}", "UserEntity", keyItem); }

        private static string ComposeIndexKey() { return string.Format("{0}", "UsersIndexList"); }

        private static string ComposeEmailIndexKey() { return string.Format("{0}:{1}", "User", "Emails"); }

        private static string ComposeUserIdIndexKey() { return string.Format("{0}:{1}", "User", "Ids"); }

        #endregion
    }
}