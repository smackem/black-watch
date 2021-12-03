using System;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Core.Util;
using BlackWatch.Daemon.Cron;
using Cronos;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Daemon.Features.CronActions
{
    public class TrackerDownloadTriggerAction : CronAction
    {
        private readonly IDataStore _dataStore;
        private readonly ILogger _logger;

        public TrackerDownloadTriggerAction(CronExpression cronExpr, IDataStore dataStore, ILogger logger)
            : base(cronExpr, "download trackers")
        {
            _dataStore = dataStore;
            _logger = logger;
        }

        public override async Task<bool> ExecuteAsync()
        {
            var jobInfo = RequestInfo.DownloadTrackers(new TrackersRequest(DateTimeOffset.UtcNow.AddDays(-1)));
            _logger.LogInformation("queue job: download trackers {JobInfo}", jobInfo);

            await _dataStore.EnqueueJobAsync(jobInfo).Linger();

            return true; // always continue
        }
    }
}
