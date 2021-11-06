using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Core.Util;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace BlackWatch.Core.Services
{
    public class RedisDataStore : IDataStore, IDisposable
    {
        private readonly ILogger<RedisDataStore> _logger;
        private readonly SemaphoreSlim _semaphore = new(1);
        private readonly string _redisConnectionString;
        private volatile ConnectionMultiplexer? _redis;

        public RedisDataStore(string redisConnectionString, ILogger<RedisDataStore> logger)
        {
            _redisConnectionString = redisConnectionString;
            _logger = logger;
        }

        public async Task InsertTrackersAsync(IEnumerable<Tracker> trackers)
        {
            var db = await GetDatabaseAsync().Linger();
            var entries = trackers
                .Select(t => new HashEntry(t.Symbol, JsonSerializer.SerializeToUtf8Bytes(t)))
                .ToArray();
            await db.HashSetAsync(RedisKeys.Trackers, entries).Linger();
        }

        public async Task<Tracker[]> GetTrackersAsync()
        {
            var db = await GetDatabaseAsync().Linger();
            var entries = await db.HashValuesAsync(RedisKeys.Trackers);
            return entries
                .Where(e => e.HasValue)
                .Select(e => JsonSerializer.Deserialize<Tracker>((byte[]) e)!)
                .ToArray();
        }

        public async Task<Quote?> GetQuoteAsync(string symbol, DateTimeOffset date)
        {
            var db = await GetDatabaseAsync().Linger();
            var key = GetDateKey(date);
            var hash = RedisKeys.Quotes.Append(symbol);
            var value = await db.HashGetAsync(hash, key).Linger();

            if (value.HasValue == false)
            {
                _logger.LogWarning("no quote found @{Hash}[{Date}]", hash, key);
                return null;
            }

            _logger.LogDebug("got value: {Quote}", value);
            return JsonSerializer.Deserialize<Quote>((byte[]) value);
        }

        public async Task SetQuoteAsync(Quote quote)
        {
            var db = await GetDatabaseAsync().Linger();
            var value = JsonSerializer.SerializeToUtf8Bytes(quote);
            var hash = RedisKeys.Quotes.Append(quote.Symbol);
            var key = GetDateKey(quote.Date);
            await db.HashSetAsync(hash, key, value).Linger();
            _logger.LogDebug("quote set @{Hash}[{Date}]", hash, key);
        }

        public void Dispose()
        {
            _redis?.Dispose();
            GC.SuppressFinalize(this);
        }

        private static RedisValue GetDateKey(DateTimeOffset date) => $"{date:yyyy-MM-dd}";

        private async Task<IDatabaseAsync> GetDatabaseAsync()
        {
            if (_redis == null)
            {
                try
                {
                    await _semaphore.WaitAsync().Linger();

                    if (_redis == null)
                    {
                        _redis = await ConnectionMultiplexer.ConnectAsync(_redisConnectionString).Linger();
                    }
                }
                finally
                {
                    _semaphore.Release(1);
                }
            }

            return _redis.GetDatabase();
        }

        private static class RedisKeys
        {
            public static readonly RedisKey Trackers = new("black-watch:symbols");
            public static readonly RedisKey Quotes = new("black-watch:quotes");
        }
    }
}
