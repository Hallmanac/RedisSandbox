using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using RedisSandbox.Console.Core;
using RedisSandbox.Console.Identity;
using RedisSandbox.Console.Identity.Cache;

namespace RedisSandbox.Console
{
    public class Runtime
    {
        public async Task InitializeUsersAsync(int numberOfUsers = 20, int numberOfPhoneNumbers = 20, int iteration = 0)
        {
            var userCache = AppConst.AppContainer.Resolve<UserCache>();
            var sw = new Stopwatch();
            
            sw.Restart();
            await userCache.ClearCacheAsync();
            sw.Stop();
            System.Console.WriteLine("Cleared Cache in {0} milliseconds", sw.ElapsedMilliseconds);

            // --- First delete existing users --- //
            sw.Restart();
            var existingUsers = await userCache.GetAllUsersInCacheAsync();
            //var existingUsers = userCache.GetAllUsersInCache().ToList();
            sw.Stop();
            System.Console.WriteLine("\nGot all existing users in {0} milliseconds.", sw.ElapsedMilliseconds);
            if(existingUsers.Count > 0)
            {
                sw.Restart();
                existingUsers.ForEach(async usr =>  await userCache.RemoveUserFromCacheAsync(usr));
                sw.Stop();
                System.Console.WriteLine("\nRemoved all existing users in {0} milliseconds", sw.ElapsedMilliseconds);
            }

            // Create all the phone numbers
            var phoneNumbers = new List<PhoneNumber>();
            for(var i = 0; i < numberOfPhoneNumbers; i++)
            {
                phoneNumbers.Add(new PhoneNumber
                {
                    AreaCode = 407,
                    PrefixNumber = 616,
                    LineNumber = 9600 + (20 - i)
                });
            }
            
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
                if(phoneCount % numberOfPhoneNumbers == 0)
                    phoneCount = 2;
                await userCache.PutUserInCacheAsync(user);
            }
            System.Console.WriteLine("\nUsers initialized in iteration {0}.", iteration);
        }

        public async Task<User> ShowUserByEmailAsync(string emailAddress, int iteration)
        {
            var userCache = AppConst.AppContainer.Resolve<UserCache>();
            if(!string.IsNullOrEmpty(emailAddress))
                return await userCache.GetByEmailAsync(emailAddress);
            var sw = new Stopwatch();
            sw.Start();
            var allUserList = await userCache.GetAllUsersInCacheAsync();
            var count = allUserList.Count;
            sw.Stop();
            System.Console.WriteLine("\nGot count of all users in {0} milliseconds. --> Iteration {1}", sw.ElapsedMilliseconds, iteration);
            sw.Reset();
            var random = new Random();
            var emailNumber = random.Next(count);
            emailAddress = string.Format("First_{0:0000}@FakeEmail.com", emailNumber);
            sw.Start();
            var returnUser = await userCache.GetByEmailAsync(emailAddress);
            sw.Stop();
            System.Console.WriteLine("\nGot random user in {0} milliseconds. --> Iteration {1}", sw.ElapsedMilliseconds, iteration);
            return returnUser;
        }
    }
}