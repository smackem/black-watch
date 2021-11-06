using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Daemon.Features.Polygon;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Daemon
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IDataStore _dataStore;
        private readonly IPolygonApiClient _polygon;
        private readonly JobQueue _jobQueue;

        public Worker(ILogger<Worker> logger, IDataStore dataStore, IPolygonApiClient polygon, JobQueue jobQueue)
        {
            _logger = logger;
            _dataStore = dataStore;
            _polygon = polygon;
            _jobQueue = jobQueue;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _jobQueue.EnqueueAsync(new Job("download crypto trackers", async ct =>
            {
                var trackerPrices = await _polygon.GetGroupedDailyCryptoPricesAsync(DateTimeOffset.Now.AddDays(-1));
                _logger.LogDebug("{Response}", trackerPrices);

                var trackers = trackerPrices.Results
                    .Select(tp => new Tracker(tp.Symbol, null, null))
                    .ToArray();

                await _dataStore.InsertTrackersAsync(trackers);

                var toDate = DateTimeOffset.Now;
                var fromDate = toDate.AddDays(-100);
                foreach (var tracker in trackers)
                {
                    await _jobQueue.EnqueueAsync(new Job($"download aggregates for {tracker.Symbol}", async _ =>
                    {
                        var prices = await _polygon.GetAggregateCryptoPricesAsync(tracker.Symbol, fromDate, toDate);
                        var quotes = prices.Results
                            .Select(p => new Quote(tracker.Symbol, p.Open, p.Close, p.High, 0, "", DateTimeOffset.FromUnixTimeMilliseconds(p.Timestamp)));

                        foreach (var quote in quotes)
                        {
                            await _dataStore.SetQuoteAsync(quote);
                        }
                    }), ct);
                }
            }), stoppingToken);

            await RunJobQueue(_jobQueue.DequeueAsync(stoppingToken), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                // _logger.LogInformation("Worker running at: {Time}", DateTimeOffset.Now);
                // var now = DateTimeOffset.Now;
                // var y = await _polygon.GetAggregateCryptoPricesAsync("X:BTCUSD", now.AddDays(-100), now);
                // _logger.LogDebug("{Response}", y);
                // await _dataStore.SetQuoteAsync(new Quote("BTCUSD", 1000, 1100, 1150, 950, "USD", DateTimeOffset.Now));
                // await _dataStore.GetQuoteAsync("BTCUSD", DateTimeOffset.Now);
                //
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private static async Task RunJobQueue(IAsyncEnumerable<Job> jobs, CancellationToken ct)
        {
            await foreach (var job in jobs.WithCancellation(ct))
            {
                await job.ExecuteAsync(ct);
            }
        }
    }
}
