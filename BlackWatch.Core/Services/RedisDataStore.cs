using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Core.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace BlackWatch.Core.Services
{
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
            IgnoreNullValues = true,
        };

        public RedisDataStore(ILogger<RedisDataStore> logger, IOptions<RedisOptions> options)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task<long> EnqueueJobsAsync(IEnumerable<JobInfo> jobs)
        {
            var db = await GetDatabaseAsync().Linger();
            var values = jobs
                .Select(Serialize)
                .ToArray();
            var count = await db.ListRightPushAsync(Names.Jobs, values).Linger();
            _logger.LogDebug("enqueued {EnqueuedJobs} jobs => queue length = {JobQueueLength}", values.Length, count);
            return count;
        }

        public Task<long> EnqueueJobAsync(JobInfo job)
        {
            return EnqueueJobsAsync(new[] { job });
        }

        public async Task<JobInfo[]> DequeueJobsAsync(int count)
        {
            var db = await GetDatabaseAsync().Linger();
            var values = await db.ListLeftPopAsync(Names.Jobs, count).Linger();
            var result = values
                .Where(v => v.HasValue)
                .Select(Deserialize<JobInfo>)
                .ToArray();
            _logger.LogDebug("dequeued {DequeuedJobs}/{EnquiredJobs} jobs", result.Length, count);
            return result;
        }

        public async Task<long> GetJobQueueLengthAsync()
        {
            var db = await GetDatabaseAsync().Linger();
            return await db.ListLengthAsync(Names.Jobs).Linger();
        }

        public async Task<string> GenerateIdAsync()
        {
            var db = await GetDatabaseAsync().Linger();
            var id = await db.StringIncrementAsync(Names.NextId).Linger();
            return id.ToString();
        }

        public async Task PutTrackersAsync(IEnumerable<Tracker> trackers)
        {
            var db = await GetDatabaseAsync().Linger();
            var entries = trackers
                .Select(t => new HashEntry(t.Symbol, Serialize(t)))
                .ToArray();
            await db.HashSetAsync(Names.Trackers, entries).Linger();
        }

        public async Task<Tracker[]> GetTrackersAsync()
        {
            var db = await GetDatabaseAsync().Linger();
            var entries = await db.HashValuesAsync(Names.Trackers).Linger();
            return entries
                .Where(e => e.HasValue)
                .Select(Deserialize<Tracker>)
                .ToArray();
        }

        public async Task<Quote?> GetQuoteAsync(string symbol, DateTimeOffset date)
        {
            var db = await GetDatabaseAsync().Linger();
            var key = Names.DateKey(date);
            var hash = Names.DailyQuotes(symbol);
            var value = await db.HashGetAsync(hash, key).Linger();

            if (value.HasValue == false)
            {
                _logger.LogWarning("no quote found @{Hash}[{Date}]", hash, key);
                return null;
            }

            _logger.LogDebug("got value: {Quote}", value);
            return Deserialize<Quote>(value);
        }

        public async Task SetQuoteAsync(Quote quote)
        {
            var db = await GetDatabaseAsync().Linger();
            var value = Serialize(quote);
            var hash = Names.DailyQuotes(quote.Symbol);
            var key = Names.DateKey(quote.Date);
            await db.HashSetAsync(hash, key, value).Linger();
            _logger.LogDebug("quote set @{Hash}[{Date}]", hash, key);
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

            _logger.LogDebug("got tally source: {TallySource}", entry);
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
            var values = await db.ListRangeAsync(key, 0, count).Linger();
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

        private async Task<IDatabase> GetDatabaseAsync()
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

            return _redis.GetDatabase();
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
            /// HASH{Symbol => Tracker}
            /// </summary>
            public static readonly RedisKey Trackers = new("black-watch:trackers");

            /// <summary>
            /// LIST{JobInfo}
            /// </summary>
            public static readonly RedisKey Jobs = new("black-watch:jobs");

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
            public static RedisKey DailyQuotes(string symbol) => $"black-watch:quotes:daily:{symbol}";

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
}
