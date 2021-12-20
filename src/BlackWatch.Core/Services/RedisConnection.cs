using System;
using System.Threading;
using System.Threading.Tasks;
using BlackWatch.Core.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace BlackWatch.Core.Services;

public class RedisConnection : IDisposable
{
    private readonly ILogger<RedisConnection> _logger;
    private readonly SemaphoreSlim _semaphore = new(1);
    private readonly RedisOptions _options;
    private volatile ConnectionMultiplexer? _redis;

    public RedisConnection(ILogger<RedisConnection> logger, IOptions<RedisOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public void Dispose()
    {
        _redis?.Dispose();
        GC.SuppressFinalize(this);
    }

    internal async Task<ConnectionMultiplexer> ConnectAsync()
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
                    _logger.LogInformation("create new connection to redis @ {ConnectionString}", _options.ConnectionString);
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
}
