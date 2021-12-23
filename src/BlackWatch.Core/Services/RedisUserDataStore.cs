using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Core.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BlackWatch.Core.Services;

/// <summary>
/// implements a <see cref="IUserDataStore"/> backed by the redis persistent cache 
/// </summary>
public class RedisUserDataStore : RedisStore, IUserDataStore
{
    private readonly ILogger<RedisUserDataStore> _logger;
    private readonly RedisOptions _options;

    public RedisUserDataStore(
        RedisConnection connection,
        ILogger<RedisUserDataStore> logger,
        IOptions<RedisOptions> options)
        : base(connection)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async IAsyncEnumerable<TallySource> GetTallySourcesAsync(string? userId)
    {
        var db = await GetDatabaseAsync().Linger();
        var pattern = RedisNames.TallySourceKey(userId ?? "*", "*");

        await foreach (var entry in db.HashScanAsync(RedisNames.TallySources, pattern).Linger())
        {
            if (entry.Value.HasValue == false)
            {
                _logger.LogWarning("HSCAN yielded an empty value @ {TallySourceKey}", entry.Name);
                continue;
            }

            yield return Deserialize<TallySource>(entry.Value);
        }
    }

    public async Task<TallySource?> GetTallySourceAsync(string userId, string id)
    {
        var db = await GetDatabaseAsync().Linger();
        var key = RedisNames.TallySourceKey(userId, id);
        var entry = await db.HashGetAsync(RedisNames.TallySources, key).Linger();

        if (entry.HasValue == false)
        {
            _logger.LogWarning("no tally source found for {TallySourceKey}", key);
            return null;
        }

        _logger.LogTrace("got tally source: {TallySource}", entry);
        return Deserialize<TallySource>(entry);
    }

    public async Task PutTallySourceAsync(string userId, TallySource tallySource)
    {
        var db = await GetDatabaseAsync().Linger();
        var key = RedisNames.TallySourceKey(userId, tallySource.Id);
        var value = Serialize(tallySource);
        await db.HashSetAsync(RedisNames.TallySources, key, value).Linger();
        _logger.LogDebug("tally source set @{Hash}[{TallySourceKey}]", RedisNames.TallySources, key);
    }

    public async Task<bool> DeleteTallySourceAsync(string userId, string id)
    {
        var db = await GetDatabaseAsync().Linger();
        var tallySourceKey = RedisNames.TallySourceKey(userId, id);
        var tallyKey = RedisNames.Tally(id);

        var tx = db.CreateTransaction();
        var deleteTallySourceTask = tx.HashDeleteAsync(RedisNames.TallySources, tallySourceKey);
        var deleteTalliesTask = tx.KeyDeleteAsync(tallyKey);

        if (await tx.ExecuteAsync().Linger() == false)
        {
            _logger.LogError("tally source removal: transaction failed @{Hash}[{TallySourceKey}]", RedisNames.TallySources, tallySourceKey);
            return false;
        }

        if (await deleteTalliesTask.Linger() == false)
        {
            _logger.LogDebug("no tallies removed for tally source @{Hash}[{TallySourceKey}]", RedisNames.TallySources, tallySourceKey);
        }

        if (await deleteTallySourceTask.Linger() == false)
        {
            _logger.LogWarning("tally source to be removed from @{Hash}[{TallySourceKey}] not found", RedisNames.TallySources, tallySourceKey);
            return false;
        }

        _logger.LogDebug("tally source removed from @{Hash}[{TallySourceKey}]", RedisNames.TallySources, tallySourceKey);
        return true;
    }

    public async Task PutTallyAsync(Tally tally)
    {
        var db = await GetDatabaseAsync();
        var key = RedisNames.Tally(tally.TallySourceId);
        var value = Serialize(tally);
        var length = await db.ListLengthAsync(key).Linger();
        var tx = db.CreateTransaction();
        var txTasks = new List<Task>
        {
            tx.ListLeftPushAsync(key, value), // push left so that most recent tally is always at position 0
        };

        if (length >= _options.MaxTallyHistoryLength)
        {
            _logger.LogDebug("tally count {TallyCount} exceeds maximum ({MaxTallyCount}), removing least recent tally @{TallyKey}",
                length, _options.MaxTallyHistoryLength, key);
            txTasks.Add(tx.ListRightPopAsync(key));
        }

        if (await tx.ExecuteAsync().Linger())
        {
            await Task.WhenAll(txTasks).Linger();
            _logger.LogDebug("tally added @{TallyKey}: {Tally}", key, tally);
        }
        else
        {
            _logger.LogError("failed to add tally @{TallyKey}: {Tally}", key, tally);
        }
    }

    public async Task<Tally[]> GetTalliesAsync(string tallySourceId, int count)
    {
        var db = await GetDatabaseAsync().Linger();
        var key = RedisNames.Tally(tallySourceId);
        var values = await db.ListRangeAsync(key, 0, count - 1).Linger();
        var tallies = values
            .Where(value => value.HasValue)
            .Select(Deserialize<Tally>)
            .ToArray();
        return tallies;
    }

    public async Task PurgeTalliesAsync(string tallySourceId)
    {
        var db = await GetDatabaseAsync().Linger();
        var key = RedisNames.Tally(tallySourceId);
        await db.KeyDeleteAsync(key);
    }
}
