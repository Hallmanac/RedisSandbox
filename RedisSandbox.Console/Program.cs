using System;
using Microsoft.Practices.Unity;
using Newtonsoft.Json;
using RedisSandbox.Console.Core;

namespace RedisSandbox.Console
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var rt = AppConst.AppContainer.Resolve<Runtime>();
            System.Console.WriteLine("\nEnter the number of users you would like to create...");
            var numberEntered = System.Console.ReadLine();

            System.Console.WriteLine("\nEnter the number of phone numbers you would like to create");
            var qtOfPhonesEntered = System.Console.ReadLine();

            int userQty;
            int phoneQty;
            Int32.TryParse(numberEntered, out userQty);
            Int32.TryParse(qtOfPhonesEntered, out phoneQty);

            userQty = userQty == 0 ? 20 : userQty;
            phoneQty = phoneQty == 0 ? 20 : phoneQty;

            rt.InitializeUsersAsync(userQty, phoneQty).Wait();

            var userByEmail = rt.ShowRandomUser();
            System.Console.WriteLine("\nThe random user is...\n{0}", JsonConvert.SerializeObject(userByEmail, Formatting.Indented));
        }
    }
}