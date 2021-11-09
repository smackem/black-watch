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
    public class RedisDataStore : IDataStore, IDisposable
    {
        private readonly ILogger<RedisDataStore> _logger;
        private readonly SemaphoreSlim _semaphore = new(1);
        private readonly RedisSettings _settings;
        private volatile ConnectionMultiplexer? _redis;
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            IgnoreNullValues = true,
        };

        public RedisDataStore(ILogger<RedisDataStore> logger, IOptions<RedisSettings> settings)
        {
            _settings = ValidateSettings(settings.Value);
            _logger = logger;
        }

        public async Task<long> EnqueueJobAsync(IEnumerable<JobInfo> jobs)
        {
            var db = await GetDatabaseAsync().Linger();
            var values = jobs
                .Select(Serialize)
                .ToArray();
            var count = await db.ListLeftPushAsync(RedisKeys.Jobs, values).Linger();
            _logger.LogDebug("enqueued {EnqueuedJobs} jobs => queue length = {JobQueueLength}", values.Length, count);
            return count;
        }

        public Task EnqueueJobAsync(JobInfo job)
        {
            return EnqueueJobAsync(new[] { job });
        }

        public async Task<JobInfo[]> DequeueJobsAsync(int count)
        {
            var db = await GetDatabaseAsync().Linger();
            var values = await db.ListRightPopAsync(RedisKeys.Jobs, count).Linger();
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
            return await db.ListLengthAsync(RedisKeys.Jobs);
        }

        public async Task InsertTrackersAsync(IEnumerable<Tracker> trackers)
        {
            var db = await GetDatabaseAsync().Linger();
            var entries = trackers
                .Select(t => new HashEntry(t.Symbol, Serialize(t)))
                .ToArray();
            await db.HashSetAsync(RedisKeys.Trackers, entries).Linger();
        }

        public async Task<Tracker[]> GetTrackersAsync()
        {
            var db = await GetDatabaseAsync().Linger();
            var entries = await db.HashValuesAsync(RedisKeys.Trackers).Linger();
            return entries
                .Where(e => e.HasValue)
                .Select(Deserialize<Tracker>)
                .ToArray();
        }

        public async Task<Quote?> GetQuoteAsync(string symbol, DateTimeOffset date)
        {
            var db = await GetDatabaseAsync().Linger();
            var key = GetDateKey(date);
            var hash = RedisKeys.Quotes.Join(symbol);
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
            var hash = RedisKeys.Quotes.Join(quote.Symbol);
            var key = GetDateKey(quote.Date);
            await db.HashSetAsync(hash, key, value).Linger();
            _logger.LogDebug("quote set @{Hash}[{Date}]", hash, key);
        }

        public void Dispose()
        {
            _redis?.Dispose();
            GC.SuppressFinalize(this);
        }

        private static RedisSettings ValidateSettings(RedisSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                throw new ArgumentException("redis connection string must not be empty");
            }

            return settings;
        }

        private static RedisValue GetDateKey(DateTimeOffset date) => $"{date:yyyy-MM-dd}";

        private async Task<IDatabase> GetDatabaseAsync()
        {
            if (_redis == null)
            {
                try
                {
                    await _semaphore.WaitAsync().Linger();

                    if (_redis == null)
                    {
                        _redis = await ConnectionMultiplexer.ConnectAsync(_settings.ConnectionString).Linger();
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

        private static class RedisKeys
        {
            public static readonly RedisKey Trackers = new("black-watch:symbols");
            public static readonly RedisKey Quotes = new("black-watch:quotes");
            public static readonly RedisKey Jobs = new("black-watch:jobs");
        }
    }
}
