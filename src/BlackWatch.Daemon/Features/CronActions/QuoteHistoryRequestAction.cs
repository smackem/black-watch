using System;
using System.Linq;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Core.Util;
using BlackWatch.Daemon.Cron;
using Cronos;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Daemon.Features.CronActions
{
    internal class QuoteHistoryRequestAction : CronAction
    {
        private readonly IDataStore _dataStore;
        private readonly ILogger _logger;
        private readonly int _quoteHistoryDays;

        public QuoteHistoryRequestAction(CronExpression cronExpr, IDataStore dataStore, ILogger logger, int quoteHistoryDays)
            : base(cronExpr, "trigger download of quote history")
        {
            _dataStore = dataStore;
            _logger = logger;
            _quoteHistoryDays = quoteHistoryDays;
        }

        public override async Task<bool> ExecuteAsync()
        {
            var requestInfo = RequestInfo.DownloadTrackers(
                new TrackerRequestInfo(DateTimeOffset.UtcNow.AddDays(-1), _quoteHistoryDays),
                ApiTags.Polygon);
            _logger.LogInformation("queue request: download trackers {RequestInfo}", requestInfo);

            await _dataStore.EnqueueRequestAsync(requestInfo).Linger();

            return true; // always continue
        }
    }
}
