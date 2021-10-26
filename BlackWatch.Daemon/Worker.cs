using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Daemon
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private readonly IDistributedCache _cache;

        public Worker(ILogger<Worker> logger, IConfiguration configuration, IDistributedCache cache)
        {
            _logger = logger;
            _configuration = configuration;
            _cache = cache;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {Time}, redis @ {RedisConnectionString}",
                    DateTimeOffset.Now,
                    _configuration["Redis:ConnectionString"]);

                await _cache.SetStringAsync("BlackWatch:Ticks", Environment.TickCount.ToString(), stoppingToken);
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
