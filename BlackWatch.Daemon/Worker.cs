using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Daemon.Features.Polygon;
using BlackWatch.Daemon.Jobs;
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
            await DownloadTrackersAsync(stoppingToken);
            await DownloadQuotesAsync(stoppingToken);
            await RunJobQueue(_jobQueue.DequeueAsync(stoppingToken), stoppingToken);
            _logger.LogInformation("work done");
        }

        private async Task DownloadTrackersAsync(CancellationToken stoppingToken)
        {
            await _jobQueue.EnqueueAsync(new Job("download crypto trackers", async ctx =>
            {
                GroupedDailyCurrencyPricesResponse trackerPrices;
                try
                {
                    trackerPrices = await _polygon.GetGroupedDailyCryptoPricesAsync(DateTimeOffset.Now.AddDays(-1));
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "error getting grouped daily crypto prices");
                    ctx.Result = JobExecutionResult.Fatal;
                    return;
                }

                _logger.LogDebug("{Response}", trackerPrices);

                if (trackerPrices.Status != PolygonApiStatus.Ok)
                {
                    _logger.LogWarning("aggregate crypto prices: got non-ok status: {Response}", trackerPrices);
                }

                if (trackerPrices.Results == null)
                {
                    _logger.LogWarning("grouped daily crypto prices: got empty result set: {Response}", trackerPrices);
                    ctx.Result = JobExecutionResult.Retry;
                    return;
                }

                var trackers = trackerPrices.Results
                    .Select(tp => new Tracker(tp.Symbol, null, null))
                    .ToArray();

                await _dataStore.InsertTrackersAsync(trackers);
            }), stoppingToken);
        }

        private async Task DownloadQuotesAsync(CancellationToken stoppingToken)
        {
            var trackers = await _dataStore.GetTrackersAsync();
            var toDate = DateTimeOffset.Now;
            var fromDate = toDate.AddDays(-100);

            foreach (var tracker in trackers)
            {
                await _jobQueue.EnqueueAsync(new Job($"download aggregates for {tracker.Symbol}", async ctx =>
                {
                    ctx.Result = await DownloadQuoteAsync(tracker, fromDate, toDate);
                }), stoppingToken);
            }
        }

        private async Task<JobExecutionResult> DownloadQuoteAsync(Tracker tracker, DateTimeOffset fromDate, DateTimeOffset toDate)
        {
            AggregateCurrencyPricesResponse prices;
            try
            {
                prices = await _polygon.GetAggregateCryptoPricesAsync(tracker.Symbol, fromDate, toDate);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "error getting aggregate crypto prices for {Symbol}", tracker.Symbol);
                return JobExecutionResult.Fatal;
            }

            if (prices.Status != PolygonApiStatus.Ok)
            {
                _logger.LogWarning("aggregate crypto prices: got non-ok status: {Response}", prices);
            }

            if (prices.Results == null)
            {
                _logger.LogWarning("aggregate crypto prices: got empty result set: {Response}", prices);
                return JobExecutionResult.Retry;
            }

            var quotes = prices.Results
                .Select(p => new Quote(tracker.Symbol, p.Open, p.Close, p.High, 0, "", DateTimeOffset.FromUnixTimeMilliseconds(p.Timestamp)));

            foreach (var quote in quotes)
            {
                await _dataStore.SetQuoteAsync(quote);
            }

            return JobExecutionResult.Ok;
        }

        private async Task RunJobQueue(IAsyncEnumerable<Job> jobs, CancellationToken ct)
        {
            await foreach (var job in jobs.WithCancellation(ct))
            {
                var ctx = new JobExecutionContext
                {
                    StoppingToken = ct,
                };

                await job.ExecuteAsync(ctx);

                var logLevel = ctx.Result switch
                {
                    JobExecutionResult.Ok => LogLevel.Information,
                    JobExecutionResult.Retry => LogLevel.Warning,
                    JobExecutionResult.Fatal => LogLevel.Error,
                    _ => throw new ArgumentOutOfRangeException($"unknown execution result: {ctx.Result}"),
                };

                _logger.Log(logLevel, "job {Job} executed with result: {JobExecutionResult}", job, ctx.Result);

                if (ctx.Result == JobExecutionResult.Retry)
                {
                    await _jobQueue.EnqueueAsync(job, ct);
                }
            }
        }
    }
}
