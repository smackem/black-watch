using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Core.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace BlackWatch.Core.Services;

/// <summary>
/// implements a <see cref="IDataStore"/> backed by the redis persistent cache 
/// </summary>
public class RedisDataStore : IDataStore, IDisposable
{
    private readonly ILogger<RedisDataStore> _logger;
    private readonly SemaphoreSlim _semaphore = new(1);
    private readonly RedisOptions _options;
    private volatile ConnectionMultiplexer? _redis;
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
    };

    public RedisDataStore(ILogger<RedisDataStore> logger, IOptions<RedisOptions> options)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task EnqueueRequestsAsync(IEnumerable<RequestInfo> requests)
    {
        var db = await GetDatabaseAsync().Linger();
        var groups = requests.GroupBy(request => request.ApiTag);

        foreach (var group in groups)
        {
            if (group.Key == null)
            {
                throw new ArgumentException("requests without api tag found", nameof(requests));
            }

            var values = group
                .Select(Serialize)
                .ToArray();

            var count = await db.ListRightPushAsync(Names.Requests(group.Key), values).Linger();
            _logger.LogDebug(
                "enqueued {EnqueuedJobs} requests for {ApiTag} => total queue length = {JobQueueLength}",
                values.Length, group.Key, count);
        }
    }

    public Task EnqueueRequestAsync(RequestInfo request)
    {
        return EnqueueRequestsAsync(new[] { request });
    }

    public async Task<RequestInfo[]> DequeueRequestsAsync(int count, string apiTag)
    {
        var db = await GetDatabaseAsync().Linger();
        var values = await db.ListLeftPopAsync(Names.Requests(apiTag), count).Linger();
        var result = values
            .Where(v => v.HasValue)
            .Select(Deserialize<RequestInfo>)
            .ToArray();
        _logger.LogDebug("dequeued {DequeuedJobs}/{EnquiredJobs} jobs", result.Length, count);
        return result;
    }

    public async Task<long> GetRequestQueueLengthAsync(string apiTag)
    {
        var db = await GetDatabaseAsync().Linger();
        return await db.ListLengthAsync(Names.Requests(apiTag)).Linger();
    }

    public async Task<string> GenerateIdAsync()
    {
        var db = await GetDatabaseAsync().Linger();
        var id = await db.StringIncrementAsync(Names.NextId).Linger();
        return id.ToString();
    }

    public Task<IReadOnlyCollection<Tracker>> GetDailyTrackersAsync()
    {
        return GetTrackersAsync(Names.DailyQuotes("*"), Names.DailyQuotesRegex(@"(\w+)"));
    }

    public Task<IReadOnlyCollection<Tracker>> GetHourlyTrackersAsync()
    {
        return GetTrackersAsync(Names.HourlyQuotes("*"), Names.HourlyQuotesRegex(@"(\w+)"));
    }

    public async Task<Quote?> GetDailyQuoteAsync(string symbol, DateTimeOffset date)
    {
        var db = await GetDatabaseAsync().Linger();
        var key = Names.DateKey(date);
        var hash = Names.DailyQuotes(symbol);
        var value = await db.HashGetAsync(hash, key).Linger();

        if (value.HasValue == false)
        {
            _logger.LogWarning("no daily quote found @{Hash}[{Date}]", hash, key);
            return null;
        }

        _logger.LogTrace("got daily quote: {Quote}", value);
        return Deserialize<Quote>(value);
    }

    public async Task RemoveDailyQuotesAsync(string symbol, DateTimeOffset threshold)
    {
        var db = await GetDatabaseAsync().Linger();
        var keys = new List<RedisValue>();
        var hash = Names.DailyQuotes(symbol);
        await foreach (var entry in db.HashScanAsync(hash).Linger())
        {
            var date = DateTimeOffset.Parse((string) entry.Name, styles: DateTimeStyles.AssumeUniversal);
            if (date < threshold)
            {
                keys.Add(entry.Name);
            }
        }
        var count = await db.HashDeleteAsync(hash, keys.ToArray()).Linger();
        _logger.LogInformation("removed {Count} quotes from hash @{Key}", count, hash);
    }

    public async Task<Quote?> GetHourlyQuoteAsync(string symbol, int hourOffset)
    {
        if (hourOffset > 0)
        {
            throw new ArgumentOutOfRangeException(nameof(hourOffset), "value must be <= 0");
        }

        var index = -hourOffset;
        var db = await GetDatabaseAsync().Linger();
        var key = Names.HourlyQuotes(symbol);
        var value = await db.ListGetByIndexAsync(key, index);

        if (value.HasValue == false)
        {
            _logger.LogWarning("no hourly quote found @{List}[{Index}]", key, index);
            return null;
        }

        _logger.LogTrace("got hourly quote: {Quote}", value);
        return Deserialize<Quote>(value);
    }

    public async Task PutDailyQuoteAsync(Quote quote)
    {
        var db = await GetDatabaseAsync().Linger();
        var value = Serialize(quote);
        var hash = Names.DailyQuotes(quote.Symbol);
        var key = Names.DateKey(quote.Date);
        await db.HashSetAsync(hash, key, value).Linger();
        _logger.LogDebug("daily quote set @{Hash}[{Date}]", hash, key);
    }

    public async Task PutHourlyQuoteAsync(Quote quote)
    {
        var db = await GetDatabaseAsync().Linger();
        var value = Serialize(quote);
        var key = Names.HourlyQuotes(quote.Symbol);
        var count = await db.ListLeftPushAsync(key, value).Linger();
        _logger.LogDebug("hourly quote prepended to {Key}", key);

        var maxCount = _options.MaxHourlyQuotes;
        if (count > maxCount)
        {
            await db.ListTrimAsync(key, 0, count - 1).Linger();
            _logger.LogDebug("hourly quotes @ {Key} trimmed from {OriginalCount} to {NewCount}", key, count, maxCount);
        }
    }

    public async IAsyncEnumerable<TallySource> GetTallySourcesAsync(string? userId)
    {
        var db = await GetDatabaseAsync().Linger();
        var pattern = Names.TallySourceKey(userId ?? "*", "*");

        await foreach (var entry in db.HashScanAsync(Names.TallySources, pattern).Linger())
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
        var key = Names.TallySourceKey(userId, id);
        var entry = await db.HashGetAsync(Names.TallySources, key).Linger();

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
        var key = Names.TallySourceKey(userId, tallySource.Id);
        var value = Serialize(tallySource);
        await db.HashSetAsync(Names.TallySources, key, value).Linger();
        _logger.LogDebug("tally source set @{Hash}[{TallySourceKey}]", Names.TallySources, key);
    }

    public async Task<bool> DeleteTallySourceAsync(string userId, string id)
    {
        var db = await GetDatabaseAsync().Linger();
        var tallySourceKey = Names.TallySourceKey(userId, id);
        var tallyKey = Names.Tally(id);

        var tx = db.CreateTransaction();
        var deleteTallySourceTask = tx.HashDeleteAsync(Names.TallySources, tallySourceKey);
        var deleteTalliesTask = tx.KeyDeleteAsync(tallyKey);

        if (await tx.ExecuteAsync().Linger() == false)
        {
            _logger.LogError("tally source removal: transaction failed @{Hash}[{TallySourceKey}]", Names.TallySources, tallySourceKey);
            return false;
        }

        if (await deleteTalliesTask.Linger() == false)
        {
            _logger.LogDebug("no tallies removed for tally source @{Hash}[{TallySourceKey}]", Names.TallySources, tallySourceKey);
        }

        if (await deleteTallySourceTask.Linger() == false)
        {
            _logger.LogWarning("tally source to be removed from @{Hash}[{TallySourceKey}] not found", Names.TallySources, tallySourceKey);
            return false;
        }

        _logger.LogDebug("tally source removed from @{Hash}[{TallySourceKey}]", Names.TallySources, tallySourceKey);
        return true;
    }

    public async Task PutTallyAsync(Tally tally)
    {
        var db = await GetDatabaseAsync();
        var key = Names.Tally(tally.TallySourceId);
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
        var key = Names.Tally(tallySourceId);
        var values = await db.ListRangeAsync(key, 0, count - 1).Linger();
        var tallies = values
            .Where(value => value.HasValue)
            .Select(Deserialize<Tally>)
            .ToArray();
        return tallies;
    }

    public void Dispose()
    {
        _redis?.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<IReadOnlyCollection<Tracker>> GetTrackersAsync(string redisPattern, string regexPattern)
    {
        var trackers = new List<Tracker>();
        var regex = new Regex(regexPattern, RegexOptions.Compiled);
        await foreach (var key in ScanKeysAsync(redisPattern))
        {
            var match = regex.Match(key);
            if (match.Success == false)
            {
                _logger.LogWarning(
                    "{Key} did not match pattern {RedisPattern} (regex {RegexPattern})",
                    key, redisPattern, regexPattern);
                continue;
            }

            var symbol = match.Groups[1].Value;
            trackers.Add(new Tracker(symbol));
        }

        return trackers;
    }

    private async IAsyncEnumerable<RedisKey> ScanKeysAsync(RedisValue pattern)
    {
        var redis = await ConnectRedisAsync().Linger();
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

    private async Task<ConnectionMultiplexer> ConnectRedisAsync()
    {
        // ReSharper disable once InvertIf
        if (_redis == null)
        {
            try
            {
                await _semaphore.WaitAsync().Linger();

                // ReSharper disable once ConvertIfStatementToNullCoalescingAssignment
                if (_redis == null)
                {
                    _redis = await ConnectionMultiplexer.ConnectAsync(_options.ConnectionString).Linger();
                }
            }
            finally
            {
                _semaphore.Release(1);
            }
        }

        return _redis;
    }

    private async Task<IDatabase> GetDatabaseAsync()
    {
        var redis = await ConnectRedisAsync().Linger();
        return redis.GetDatabase();
    }

    private static RedisValue Serialize<T>(T obj)
    {
        return (RedisValue) JsonSerializer.SerializeToUtf8Bytes(obj, SerializerOptions);
    }

    private static T Deserialize<T>(RedisValue value)
    {
        return JsonSerializer.Deserialize<T>((byte[]) value, SerializerOptions)!;
    }

    private static class Names
    {
        /// <summary>
        /// LIST{RequestInfo}
        /// </summary>
        public static RedisKey Requests(string apiTag) => new($"black-watch:requests:{apiTag}");

        /// <summary>
        /// HASH{TallySourceKey => TallySource}
        /// </summary>
        public static readonly RedisKey TallySources = new($"black-watch:tally-sources");

        /// <summary>
        /// STRING
        /// </summary>
        public static RedisValue TallySourceKey(string userId, string tallySourceId) => $"user-{userId}:{tallySourceId}";

        /// <summary>
        /// STRING
        /// </summary>
        public static RedisValue DateKey(DateTimeOffset date) => $"{date:yyyy-MM-dd}";

        /// <summary>
        /// HASH{Date => Quote}
        /// </summary>
        public static RedisKey DailyQuotes(string symbol) => $"{DailyQuotesPrefix}{symbol}";

        /// <summary>
        /// HASH{Date => Quote}
        /// </summary>
        public static RedisKey DailyQuotesRegex(string pattern) => $"{Regex.Escape(DailyQuotesPrefix)}{pattern}";

        private const string DailyQuotesPrefix = "black-watch:quotes:daily:";

        /// <summary>
        /// LIST{Quote}
        /// </summary>
        public static RedisKey HourlyQuotes(string symbol) => $"{HourlyQuotesPrefix}{symbol}";

        /// <summary>
        /// LIST{Quote}
        /// </summary>
        public static RedisKey HourlyQuotesRegex(string pattern) => $"{Regex.Escape(HourlyQuotesPrefix)}{pattern}";

        private const string HourlyQuotesPrefix = "black-watch:quotes:hourly:";

        /// <summary>
        /// LIST{Tally}
        /// </summary>
        public static RedisKey Tally(string tallySourceId) => $"black-watch:tally-{tallySourceId}";

        /// <summary>
        /// INTEGER
        /// </summary>
        public static readonly RedisKey NextId = new("black-watch:next-id");
    }
}