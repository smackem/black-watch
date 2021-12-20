using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using BlackWatch.Core.Util;
using StackExchange.Redis;

namespace BlackWatch.Core.Services;

public abstract class RedisStore
{
    private readonly RedisConnection _connection;
    
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
    };

    protected RedisStore(RedisConnection connection)
    {
        _connection = connection;
    }

    protected async Task<IDatabase> GetDatabaseAsync()
    {
        var redis = await _connection.ConnectAsync().Linger();
        return redis.GetDatabase();
    }

    protected async IAsyncEnumerable<RedisKey> ScanKeysAsync(RedisValue pattern)
    {
        var redis = await _connection.ConnectAsync().Linger();
        var endPoints = redis.GetEndPoints();

        foreach (var endPoint in endPoints)
        {
            var server = redis.GetServer(endPoint);

            await foreach (var key in server.KeysAsync(pattern: pattern))
            {
                yield return key;
            }
        }
    }

    protected static RedisValue Serialize<T>(T obj)
    {
        return (RedisValue) JsonSerializer.SerializeToUtf8Bytes(obj, SerializerOptions);
    }

    protected static T Deserialize<T>(RedisValue value)
    {
        return JsonSerializer.Deserialize<T>((byte[]) value, SerializerOptions)!;
    }
}
