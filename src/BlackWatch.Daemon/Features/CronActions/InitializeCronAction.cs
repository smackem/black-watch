using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Core.Util;
using BlackWatch.Daemon.Cron;
using Cronos;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Daemon.Features.CronActions
{
    internal class InitializeCronAction : CronAction
    {
        private readonly IDataStore _dataStore;
        private readonly ILogger _logger;
        private readonly QuoteDownloadTriggerAction _quoteDownloader;
        private readonly TrackerDownloadTriggerAction _trackerDownloader;
        private bool _trackersDownloadQueued;

        public InitializeCronAction(
            CronExpression cronExpr,
            IDataStore dataStore,
            ILogger logger,
            QuoteDownloadTriggerAction quoteDownloader,
            TrackerDownloadTriggerAction trackerDownloader)
            : base(cronExpr, "trigger initial trackers download")
        {
            _dataStore = dataStore;
            _logger = logger;
            _quoteDownloader = quoteDownloader;
            _trackerDownloader = trackerDownloader;
        }

        public override async Task<bool> ExecuteAsync()
        {
            var jobQueueLength = await _dataStore.GetJobQueueLengthAsync();
            if (jobQueueLength > 0)
            {
                _logger.LogWarning("initial job queue length at startup: {JobQueueLength}", jobQueueLength);
            }

            var trackers = await _dataStore.GetTrackersAsync().Linger();
            if (trackers.Length > 0)
            {
                // queue quote download and quit
                await _quoteDownloader.ExecuteAsync().Linger();
                return false;
            }

            // ReSharper disable once InvertIf
            if (_trackersDownloadQueued == false)
            {
                await _trackerDownloader.ExecuteAsync().Linger();
                _trackersDownloadQueued = true;
            }
            return true;
        }
    }
}
