using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Core.Util;
using BlackWatch.Daemon.Cron;
using Cronos;

namespace BlackWatch.Daemon.Features.CronActions
{
    internal class InitializeCronAction : CronAction
    {
        private readonly IDataStore _dataStore;
        private readonly QuoteDownloadTriggerAction _quoteDownloader;
        private readonly TrackerDownloadTriggerAction _trackerDownloader;
        private bool _trackersDownloadQueued;

        public InitializeCronAction(
            CronExpression cronExpr,
            IDataStore dataStore,
            QuoteDownloadTriggerAction quoteDownloader,
            TrackerDownloadTriggerAction trackerDownloader)
            : base(cronExpr, "trigger initial trackers download")
        {
            _dataStore = dataStore;
            _quoteDownloader = quoteDownloader;
            _trackerDownloader = trackerDownloader;
        }

        public override async Task<bool> ExecuteAsync()
        {
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
