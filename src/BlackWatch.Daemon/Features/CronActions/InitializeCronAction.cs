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
        private readonly QuoteHistoryRequestAction _quoteDownloader;
        private readonly TrackerRequestAction _trackerDownloader;
        private bool _trackersDownloadQueued;

        public InitializeCronAction(
            CronExpression cronExpr,
            IDataStore dataStore,
            ILogger logger,
            QuoteHistoryRequestAction quoteDownloader,
            TrackerRequestAction trackerDownloader)
            : base(cronExpr, "trigger initial trackers download")
        {
            _dataStore = dataStore;
            _logger = logger;
            _quoteDownloader = quoteDownloader;
            _trackerDownloader = trackerDownloader;
        }

        public override async Task<bool> ExecuteAsync()
        {
            var polygonRequestCount = await _dataStore.GetRequestQueueLengthAsync(ApiTags.Polygon);
            if (polygonRequestCount > 0)
            {
                _logger.LogWarning("initial polygon request queue length at startup: {JobQueueLength}", polygonRequestCount);
            }

            var messariRequestCount = await _dataStore.GetRequestQueueLengthAsync(ApiTags.Messari);
            if (messariRequestCount > 0)
            {
                _logger.LogWarning("initial messari request queue length at startup: {JobQueueLength}", messariRequestCount);
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
