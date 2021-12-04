using System;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Core.Util;
using BlackWatch.Daemon.Cron;
using Cronos;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Daemon.Features.CronActions
{
    public class TrackerRequestAction : CronAction
    {
        private readonly IDataStore _dataStore;
        private readonly ILogger _logger;

        public TrackerRequestAction(CronExpression cronExpr, IDataStore dataStore, ILogger logger)
            : base(cronExpr, "download trackers")
        {
            _dataStore = dataStore;
            _logger = logger;
        }

        public override async Task<bool> ExecuteAsync()
        {
            var requestInfo = RequestInfo.DownloadTrackers(new TrackerRequestInfo(DateTimeOffset.UtcNow.AddDays(-1)), ApiTags.Polygon);
            _logger.LogInformation("queue request: download trackers {RequestInfo}", requestInfo);

            await _dataStore.EnqueueRequestAsync(requestInfo).Linger();

            return true; // always continue
        }
    }
}
