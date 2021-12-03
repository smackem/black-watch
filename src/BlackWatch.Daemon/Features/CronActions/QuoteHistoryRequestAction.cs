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
            var trackers = await _dataStore.GetTrackersAsync().Linger();
            var (from, to) = DateRange.DaysUntilYesterdayUtc(_quoteHistoryDays);

            _logger.LogInformation(
                "queue job: download quote history for {TrackerCount} trackers from {FromDate} to {ToDate}",
                trackers.Length, from, to);

            var jobInfos = trackers
                .Select(t => RequestInfo.DownloadQuoteHistory(new QuoteHistoryRequestInfo(t.Symbol, from, to), ApiTags.Polygon));

            await _dataStore.EnqueueRequestsAsync(jobInfos).Linger();
            return true;
        }
    }
}