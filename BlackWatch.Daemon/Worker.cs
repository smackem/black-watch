using System;
using System.Threading;
using System.Threading.Tasks;
using BlackWatch.Core;
using BlackWatch.Core.Contracts;
using BlackWatch.Daemon.Features.Polygon;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Daemon
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IDataStore _dataStore;
        private readonly IPolygonApiClient _polygon;

        public Worker(ILogger<Worker> logger, IDataStore dataStore, IPolygonApiClient polygon)
        {
            _logger = logger;
            _dataStore = dataStore;
            _polygon = polygon;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {Time}", DateTimeOffset.Now);
                var x = await _polygon.GetGroupedDailyCryptoPricesAsync(DateTimeOffset.Now.AddDays(-1));
                _logger.LogDebug("{Response}", x);
                var now = DateTimeOffset.Now;
                var y = await _polygon.GetAggregateCryptoPricesAsync("X:BTCUSD", now.AddDays(-100), now);
                _logger.LogDebug("{Response}", y);
                await _dataStore.SetQuoteAsync(new Quote("BTCUSD", 1000, 1100, 1150, 950, "USD", DateTimeOffset.Now));
                await _dataStore.GetQuoteAsync("BTCUSD", DateTimeOffset.Now);

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
