using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Core.Util;
using BlackWatch.Daemon.Cron;
using Cronos;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Daemon.Features.CronActions;

internal class InitializeCronAction : CronAction
{
    private readonly IRequestQueue _requestQueue;
    private readonly IQuoteStore _quoteStore;
    private readonly QuoteHistoryRequestAction _historyDownloader;
    private readonly ILogger _logger;

    public InitializeCronAction(
        CronExpression cronExpr,
        IRequestQueue requestQueue,
        IQuoteStore quoteStore,
        ILogger logger,
        QuoteHistoryRequestAction historyDownloader)
        : base(cronExpr, "trigger initial trackers download")
    {
        _requestQueue = requestQueue;
        _quoteStore = quoteStore;
        _logger = logger;
        _historyDownloader = historyDownloader;
    }

    public override async Task<bool> ExecuteAsync()
    {
        var polygonRequestCount = await _requestQueue.GetRequestQueueLengthAsync(ApiTags.Polygon);
        if (polygonRequestCount > 0)
        {
            _logger.LogWarning("initial polygon request queue length at startup: {JobQueueLength}", polygonRequestCount);
        }

        var messariRequestCount = await _requestQueue.GetRequestQueueLengthAsync(ApiTags.Messari);
        if (messariRequestCount > 0)
        {
            _logger.LogWarning("initial messari request queue length at startup: {JobQueueLength}", messariRequestCount);
        }

        var trackers = await _quoteStore.GetDailyTrackersAsync().Linger();
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
