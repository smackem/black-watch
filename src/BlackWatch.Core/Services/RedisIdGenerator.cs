using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Core.Util;

namespace BlackWatch.Core.Services;

/// <summary>
/// implements a <see cref="IIdGenerator"/> backed by the redis persistent cache 
/// </summary>
public class RedisIdGenerator : RedisStore, IIdGenerator
{
    public RedisIdGenerator(RedisConnection connection)
        : base(connection)
    {}

    public async Task<string> GenerateIdAsync()
    {
        var db = await GetDatabaseAsync().Linger();
        var id = await db.StringIncrementAsync(RedisNames.NextId).Linger();
        return id.ToString();
    }
}
