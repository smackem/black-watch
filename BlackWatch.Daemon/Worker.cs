using System;
using System.Threading;
using System.Threading.Tasks;
using BlackWatch.Core;
using BlackWatch.Core.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Daemon
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IDataStore _dataStore;

        public Worker(ILogger<Worker> logger, IDataStore dataStore)
        {
            _logger = logger;
            _dataStore = dataStore;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {Time}", DateTimeOffset.Now);
                await _dataStore.SetQuoteAsync(new Quote("BTCUSD", 1000, 1100, 1150, 950, "USD", DateTimeOffset.Now));
                await _dataStore.GetQuoteAsync("BTCUSD", DateTimeOffset.Now);

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
