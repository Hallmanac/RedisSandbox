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
            var theUser = _appCache.GetItemViaIndex<User>(ComposeUserIdIndexKey(), id.ToString());
            return theUser;
        }

        public User GetByUserName(string userName) { return _appCache.GetValue<User>(ComposeKey(userName), ComposeIndexKey()); }

        public void PutUserInCache(User user)
        {
            _appCache.Put(ComposeKey(user.Username), user, TimeSpan.FromDays(30), ComposeIndexKey());

            // Set the index for Emails
            user.Emails.ForEach(eml => _appCache.SetIndex(ComposeEmailIndexKey(), new KeyValuePair<string, string>(eml.EmailAddress, ComposeKey(user.Username))));

            // Set the index for user id
            _appCache.SetIndex(ComposeUserIdIndexKey(), new KeyValuePair<string, string>(user.Id.ToString(), ComposeKey(user.Username)));
        }

        public void RemoveUserFromCache(User user)
        {
            _appCache.Remove(ComposeKey(user.Username), ComposeIndexKey());
            user.Emails.ForEach(eml => _appCache.RemoveFromIndex(ComposeEmailIndexKey(), eml.EmailAddress));
            _appCache.RemoveFromIndex(ComposeUserIdIndexKey(), user.Id.ToString());
        }

        public IEnumerable<User> GetAllUsersInCache() { return _appCache.GetAllIndexedItemsInCache<User>(ComposeIndexKey()); }

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