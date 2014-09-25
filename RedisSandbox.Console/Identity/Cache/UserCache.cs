using System;
using System.Collections.Generic;
using System.Linq;
using RedisSandbox.Console.Core.Cache;

namespace RedisSandbox.Console.Identity.Cache
{
    public class UserCache
    {
        private readonly IAppCache _appCache;

        public UserCache() { _appCache = _appCache ?? new RedisCache(); }
        
        public UserCache (IAppCache appCache): this() { _appCache = appCache; }

        public User GetById(int id) { return _appCache.GetAllIndexedItemsInCache<User>(ComposeIndexKey()).FirstOrDefault(usr => usr.Id == id); }

        public User GetByUserName(string userName)
        {
            return _appCache.GetValue<User>(ComposeKey(userName), ComposeIndexKey());
        }

        public void PutUserInCache(User user)
        {
            _appCache.Put(ComposeKey(user.Username), user, TimeSpan.FromDays(30), ComposeIndexKey());
            
        }

        public void RemoveUserFromCache(User user) { _appCache.Remove(ComposeKey(user.Username), ComposeIndexKey()); }

        public IEnumerable<User> GetAllUsersInCache() { return _appCache.GetAllIndexedItemsInCache<User>(ComposeIndexKey()); }

        public User GetByEmail(string emailAddress)
        {
            return GetAllUsersInCache().FirstOrDefault(usr => usr.Emails.Any(eml => eml.EmailAddress == emailAddress));
        }

        public void ClearCache()
        {
            _appCache.ClearCache();
        }

        #region Key Composers

        private static string ComposeKey(string keyItem) { return string.Format("{0}_{1}", "UserEntity", keyItem); }

        private static string ComposeIndexKey() { return string.Format("{0}", "UsersIndexList"); }

        private static string ComposeChangePasswordKey(string keyItem) { return string.Format("{0}_{1}", "ChangePasswordToken", keyItem); }

        private static string ComposeChangePasswordIndexKey() { return "ChangePasswordTokensList"; }

        #endregion
    }
}