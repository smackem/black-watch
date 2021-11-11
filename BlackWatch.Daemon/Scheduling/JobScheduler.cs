using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Core.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BlackWatch.Daemon
{
    public class JobScheduler : WorkerBase
    {
        private readonly ILogger<JobScheduler> _logger;
        private readonly IDataStore _dataStore;
        private readonly JobSchedulerOptions _options;

        public JobScheduler(ILogger<JobScheduler> logger, IDataStore dataStore, IOptions<JobSchedulerOptions> options)
            : base(logger)
        {
            _logger = logger;
            _dataStore = dataStore;
            _options = options.Value;
        }

        protected override async Task ExecuteOverrideAsync(CancellationToken stoppingToken)
        {
            var jobInfo = JobInfo.GetDailyGroupedCrypto(new DailyGroupedCryptoJob(DateTimeOffset.Now));
            await _dataStore.EnqueueJobAsync(jobInfo);

            while (stoppingToken.IsCancellationRequested == false)
            {
                _logger.LogInformation("getting quotes");
                var trackers = await _dataStore.GetTrackersAsync();
                var (from, to) = DateRange.DaysUntilToday(_options.QuoteHistoryDays);
                await _dataStore.EnqueueJobAsync(trackers.Select(t =>
                    JobInfo.GetAggregateCrypto(new AggregateCryptoJob(t.Symbol, from, to))));

                var delay = trackers.Length > 0 ? TimeSpan.FromHours(1) : TimeSpan.FromMinutes(1);
                await Task.Delay(delay, stoppingToken);
            }
        }
   }
}