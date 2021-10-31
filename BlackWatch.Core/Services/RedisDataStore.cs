using System;
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

        public async Task<Quote?> GetQuoteAsync(string symbol, DateTimeOffset date)
        {
            var db = await GetDatabaseAsync();
            var key = GetSymbolKey(symbol, date);
            var value = await db.HashGetAsync(RedisKeys.Symbols, key);

            if (value.HasValue == false)
            {
                _logger.LogWarning("no quote found for symbol: {Key}", key);
                return null;
            }

            _logger.LogDebug("got value: {Quote}", value);
            return JsonSerializer.Deserialize<Quote>((byte[]) value);
        }

        public async Task SetQuoteAsync(Quote quote)
        {
            var db = await GetDatabaseAsync();
            var value = JsonSerializer.SerializeToUtf8Bytes(quote);
            var key = GetSymbolKey(quote.Symbol, quote.Date);
            await db.HashSetAsync(RedisKeys.Symbols, key, value);
            _logger.LogDebug("quote set for symbol: {Symbol}", quote.Symbol);
        }

        public void Dispose()
        {
            _redis?.Dispose();
            GC.SuppressFinalize(this);
        }

        private static RedisValue GetSymbolKey(string symbol, DateTimeOffset date) => $"{symbol}:{date:yyyy-MM-dd}";

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
            public static readonly RedisKey Symbols = new("black-watch:symbols");
        }
    }
}
