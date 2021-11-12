using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Core.Util;
using BlackWatch.Daemon.Util;
using Cronos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BlackWatch.Daemon.Features.Scheduling
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
            var trackers = await _dataStore.GetTrackersAsync().Linger();
            if (trackers.Length == 0)
            {
                await DownloadTrackersAsync();
            }

            await Task.WhenAll(
                RunCronAction("* * * * *", DownloadQuoteHistoryInitialAsync, "download-quote-history-initial", stoppingToken),
                RunCronAction(_options.Cron.DownloadTrackers, DownloadTrackersAsync, "download-trackers", stoppingToken),
                RunCronAction(_options.Cron.DownloadQuoteHistory, DownloadQuoteHistoryAsync, "download-quote-history", stoppingToken));
        }

        private async Task RunCronAction(string cronStr, Func<Task<bool>> action, string moniker, CancellationToken stoppingToken)
        {
            var cron = CronExpression.Parse(cronStr);
            _logger.LogInformation("{CronActionMoniker}: scheduled for '{CronExpr}'", moniker, cron);

            while (stoppingToken.IsCancellationRequested == false)
            {
                var now = DateTimeOffset.UtcNow;
                var occurrence = cron.GetNextOccurrence(DateTimeOffset.UtcNow, TimeZoneInfo.Utc);
                _logger.LogInformation("{CronActionMoniker}: next occurrence @ '{CronDate}'", moniker, occurrence);

                if (occurrence == null)
                {
                    _logger.LogWarning("{CronActionMoniker}: no next occurrence => quit cron runner", moniker, occurrence);
                    break;
                }

                var delay = occurrence.Value - now;
                _logger.LogDebug("{CronActionMoniker}: waiting for {Delay} until execution", moniker, delay);
                await Task.Delay(delay, stoppingToken).Linger();

                // ReSharper disable once InvertIf
                if (await action().Linger() == false)
                {
                    _logger.LogInformation("{CronActionMoniker}: cron action signalled end", moniker);
                    break;
                }
            }
        }

        private async Task<bool> DownloadQuoteHistoryAsync()
        {
            await InternalDownloadQuoteHistoryAsync();
            return true; // always continue
        }

        private async Task<bool> DownloadQuoteHistoryInitialAsync()
        {
            return await InternalDownloadQuoteHistoryAsync() == 0; // continue if no trackers found
        }

        private async Task<int> InternalDownloadQuoteHistoryAsync()
        {
            var trackers = await _dataStore.GetTrackersAsync().Linger();
            var (from, to) = DateRange.DaysUntilYesterdayUtc(_options.QuoteHistoryDays);

            _logger.LogInformation(
                "queue job: download quote history for {TrackerCount} trackers from {FromDate} to {ToDate}",
                trackers.Length, from, to);

            await _dataStore.EnqueueJobAsync(trackers.Select(t =>
                JobInfo.GetAggregateCrypto(new AggregateCryptoJob(t.Symbol, from, to)))).Linger();

            return trackers.Length;
        }

        private async Task<bool> DownloadTrackersAsync()
        {
            var jobInfo = JobInfo.GetDailyGroupedCrypto(new DailyGroupedCryptoJob(DateTimeOffset.UtcNow.AddDays(-1)));
            _logger.LogInformation("queue job: download trackers {JobInfo}", jobInfo);

            await _dataStore.EnqueueJobAsync(jobInfo).Linger();

            return true; // always continue
        }
    }
}