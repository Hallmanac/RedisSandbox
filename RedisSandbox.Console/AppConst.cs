namespace RedisSandbox.Console
{
    public class AppConst
    {
#if !DEBUG
        public const string RedisConnectionString = "ajunct.redis.cache.windows.net,ssl=true,password=X/vC00bUpgJSgVBEu7/2hGrYwx/vaSCa8KfBb52r1VQ=";
#else
        public const string RedisConnectionString = "localhost";
#endif
 
    }
}