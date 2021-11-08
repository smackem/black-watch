using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Daemon.Features.Polygon;
using BlackWatch.Daemon.Jobs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Daemon
{
    public class QuoteDownloadWorker : WorkerBase
    {
        private readonly ILogger<QuoteDownloadWorker> _logger;
        private readonly IDataStore _dataStore;
        private readonly IPolygonApiClient _polygon;
        private readonly JobQueue _jobQueue;
        
        public QuoteDownloadWorker(ILogger<QuoteDownloadWorker> logger, IDataStore dataStore, IPolygonApiClient polygon, JobQueue jobQueue)
            : base(logger)
        {
            _logger = logger;
            _dataStore = dataStore;
            _polygon = polygon;
            _jobQueue = jobQueue;
        }
        
        protected override async Task ExecuteOverrideAsync(CancellationToken stoppingToken)
        {
            while (stoppingToken.IsCancellationRequested == false)
            {
                _logger.LogDebug("downloading quotes...");
                var jobsQueued = await QueueWorkAsync(stoppingToken);
                var delay = jobsQueued ? TimeSpan.FromHours(1) : TimeSpan.FromMinutes(1);
                _logger.LogDebug("download next quotes in {Delay}", delay);
                await Task.Delay(delay, stoppingToken);
            }
        }

        private async Task<bool> QueueWorkAsync(CancellationToken stoppingToken)
        {
            var trackers = await _dataStore.GetTrackersAsync();
            var toDate = DateTimeOffset.Now;
            var fromDate = toDate.AddDays(-100);
            var jobsQueued = false;

            foreach (var tracker in trackers)
            {
                await _jobQueue.EnqueueAsync(
                    new Job($"download aggregates for {tracker.Symbol}",
                        _ => DownloadQuoteAsync(tracker, fromDate, toDate)),
                    stoppingToken);

                jobsQueued = true;
            }

            return jobsQueued;
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
    }
}