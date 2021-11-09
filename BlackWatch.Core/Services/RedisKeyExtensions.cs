using StackExchange.Redis;

namespace BlackWatch.Core.Services
{
    public static class RedisKeyExtensions
    {
        public static RedisKey Join(this RedisKey key, string suffix)
        {
            return key.Append(":" + suffix);
        }
    }
}