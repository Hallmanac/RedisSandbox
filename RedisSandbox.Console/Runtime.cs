using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using RedisSandbox.Console.Core;
using RedisSandbox.Console.Identity;
using RedisSandbox.Console.Identity.Cache;

namespace RedisSandbox.Console
{
    public class Runtime
    {
        public async Task InitializeUsersAsync(int numberOfUsers = 20, int numberOfPhoneNumbers = 20)
        {
            var userCache = AppConst.AppContainer.Resolve<UserCache>();
            userCache.ClearCache();
            // --- First delete existing users --- //
            var existingUsers = userCache.GetAllUsersInCache().ToList();

            if(existingUsers.Count > 0)
            {
                existingUsers.ForEach(userCache.RemoveUserFromCache);
            }

            // Create all the phone numbers
            var phoneNumbers = new List<PhoneNumber>();
            for (var i = 0; i < numberOfPhoneNumbers; i++)
            {
                phoneNumbers.Add(new PhoneNumber
                {
                    AreaCode = 407,
                    PrefixNumber = 616,
                    LineNumber = 9600 + (20 - i)
                });
            }

            var users = new List<User>();
            var phoneCount = 2;
            for(var i = 0; i < numberOfUsers; i++)
            {
                var user = new User
                {
                    Username = string.Format("UserNumber{0:0000}", i),
                    FirstName = string.Format("First{0:0000}", i),
                    LastName = string.Format("Last{0:0000}", i)
                };
                user.Emails.Add(new Email
                {
                    EmailAddress = string.Format("First_{0:0000}@FakeEmail.com", i)
                });
                var lineNumber1 = 9600 + (20 - (phoneCount));
                var lineNumber2 = lineNumber1 + 1;
                user.PhoneNumbers.Add(phoneNumbers.FirstOrDefault(ph => ph.LineNumber == lineNumber1));
                user.PhoneNumbers.Add(phoneNumbers.FirstOrDefault(ph => ph.LineNumber == lineNumber2));
                phoneCount += 2;
                if (phoneCount % numberOfPhoneNumbers == 0)
                {
                    phoneCount = 2;
                }
                userCache.PutUserInCache(user);
            }
            System.Console.WriteLine("\nUsers initialized.");
        }

        public async Task<User> ShowUserByEmailAsync(string emailAddress)
        {
            var userCache = AppConst.AppContainer.Resolve<UserCache>();
            if(!string.IsNullOrEmpty(emailAddress))
                return userCache.GetByEmail(emailAddress);
            var sw = new Stopwatch();
            sw.Start();
            var count = userCache.GetAllUsersInCache().Count();
            sw.Stop();
            System.Console.WriteLine("\nGot count of all users in {0}", sw.ElapsedMilliseconds);
            sw.Reset();
            var random = new Random();
            var emailNumber = random.Next(count);

            emailAddress = string.Format("First_{0:0000}@FakeEmail.com", emailNumber);
            sw.Start();
            var returnUser = userCache.GetByEmail(emailAddress);
            sw.Stop();
            System.Console.WriteLine("\nGot random user in {0}", sw.ElapsedMilliseconds);
            return returnUser;
        }
    }
}