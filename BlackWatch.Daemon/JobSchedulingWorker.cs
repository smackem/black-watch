using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Core.Util;
using BlackWatch.Daemon.Features.Polygon;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Daemon
{
    public class JobSchedulingWorker : WorkerBase
    {
        private readonly ILogger<JobSchedulingWorker> _logger;
        private readonly IDataStore _dataStore;

        public JobSchedulingWorker(ILogger<JobSchedulingWorker> logger, IDataStore dataStore, IPolygonApiClient polygon)
            : base(logger)
        {
            _logger = logger;
            _dataStore = dataStore;
        }

        protected override async Task ExecuteOverrideAsync(CancellationToken stoppingToken)
        {
            var jobInfo = JobInfo.GetDailyGroupedCrypto(new DailyGroupedCryptoJob(DateTimeOffset.Now));
            await _dataStore.EnqueueJobAsync(jobInfo);

            while (stoppingToken.IsCancellationRequested == false)
            {
                _logger.LogInformation("getting quotes");
                var trackers = await _dataStore.GetTrackersAsync();
                var (from, to) = DateRange.DaysUntilToday(10);
                await _dataStore.EnqueueJobAsync(trackers.Select(t =>
                    JobInfo.GetAggregateCrypto(new AggregateCryptoJob(t.Symbol, from, to))));

                var delay = trackers.Length > 0 ? TimeSpan.FromHours(1) : TimeSpan.FromMinutes(1);
                await Task.Delay(delay, stoppingToken);
            }
        }
   }
}