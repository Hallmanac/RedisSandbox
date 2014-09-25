using System;
using Microsoft.Practices.Unity;
using RedisSandbox.Console.Core.Cache;

namespace RedisSandbox.Console.Core
{
    public class UnityConfig
    {
        private static readonly Lazy<IUnityContainer> Container = new Lazy<IUnityContainer>(() =>
        {
            var container = new UnityContainer();
            container.RegisterType<IAppCache, RedisCache>();

            return container;
        });

        public static IUnityContainer GetConfiguredContainer() { return Container.Value; }
    }
}