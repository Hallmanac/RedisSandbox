using System.Collections.Generic;
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

        public async Task<User> ShowRandomUser()
        {
            var userCache = AppConst.AppContainer.Resolve<UserCache>();

            return userCache.GetByEmail(string.Format("First_{0:0000}@FakeEmail.com", 5));
        }
    }
}