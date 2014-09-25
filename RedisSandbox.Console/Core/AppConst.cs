using Microsoft.Practices.Unity;

namespace RedisSandbox.Console.Core
{
    public class AppConst
    {
        private static IUnityContainer _appContainer;
#if !DEBUG
        public const string RedisConnectionString = "[cache-name].redis.cache.windows.net,ssl=true,password=[your password here],allowAdmin=true";
#else
        public const string RedisConnectionString = "localhost,allowAdmin=true";
#endif

        public static IUnityContainer AppContainer
        {
            get
            {
                _appContainer = _appContainer ?? UnityConfig.GetConfiguredContainer();
                return _appContainer;
            }
            set
            {
                _appContainer = value;
            }
        }
    }
}