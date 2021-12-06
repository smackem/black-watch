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
        private readonly QuoteHistoryRequestAction _historyDownloader;

        public InitializeCronAction(
            CronExpression cronExpr,
            IDataStore dataStore,
            ILogger logger,
            QuoteHistoryRequestAction historyDownloader)
            : base(cronExpr, "trigger initial trackers download")
        {
            _dataStore = dataStore;
            _logger = logger;
            _historyDownloader = historyDownloader;
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

            var trackers = await _dataStore.GetDailyTrackersAsync().Linger();
            if (trackers.Count == 0)
            {
                _logger.LogInformation("no daily trackers in database, queue download");
                // queue quote history download
                await _historyDownloader.ExecuteAsync().Linger();
            }
            else
            {
                _logger.LogInformation("{TrackerCount} daily trackers in database", trackers.Count);
            }

            return false;
        }
    }
}
