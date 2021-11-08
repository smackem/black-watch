using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Daemon.Features.Polygon;
using BlackWatch.Daemon.Jobs;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Daemon
{
    public class TrackerDownloadWorker : WorkerBase
    {
        private readonly ILogger<TrackerDownloadWorker> _logger;
        private readonly IDataStore _dataStore;
        private readonly IPolygonApiClient _polygon;
        private readonly JobQueue _jobQueue;

        public TrackerDownloadWorker(ILogger<TrackerDownloadWorker> logger, IDataStore dataStore, IPolygonApiClient polygon, JobQueue jobQueue)
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
                await QueueWorkAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private Task QueueWorkAsync(CancellationToken stoppingToken)
        {
            return _jobQueue.EnqueueAsync(new Job("download crypto trackers", async _ =>
            {
                GroupedDailyCurrencyPricesResponse trackerPrices;
                try
                {
                    trackerPrices = await _polygon.GetGroupedDailyCryptoPricesAsync(DateTimeOffset.Now.AddDays(-1));
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "error getting grouped daily crypto prices");
                    return JobExecutionResult.Fatal;
                }

                _logger.LogDebug("{Response}", trackerPrices);

                if (trackerPrices.Status != PolygonApiStatus.Ok)
                {
                    _logger.LogWarning("aggregate crypto prices: got non-ok status: {Response}", trackerPrices);
                }

                if (trackerPrices.Results == null)
                {
                    _logger.LogWarning("grouped daily crypto prices: got empty result set: {Response}", trackerPrices);
                    return JobExecutionResult.Retry;
                }

                var trackers = trackerPrices.Results
                    .Select(tp => new Tracker(tp.Symbol, null, null))
                    .ToArray();

                await _dataStore.InsertTrackersAsync(trackers);
                return JobExecutionResult.Ok;
            }), stoppingToken);
        }
    }
}