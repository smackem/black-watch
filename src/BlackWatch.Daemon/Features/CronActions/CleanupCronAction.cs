using System;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Daemon.Cron;
using Cronos;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Daemon.Features.CronActions;

public class CleanupCronAction : CronAction
{
    private readonly IDataStore _dataStore;
    private readonly ILogger _logger;
    private readonly int _quoteHistoryDays;

    public CleanupCronAction(
        CronExpression cronExpr,
        IDataStore dataStore,
        ILogger logger,
        int quoteHistoryDays)
        : base(cronExpr, "cleanup")
    {
        _dataStore = dataStore;
        _logger = logger;
        _quoteHistoryDays = quoteHistoryDays;
    }

    public override async Task<bool> ExecuteAsync()
    {
        var threshold = DateTimeOffset.UtcNow.AddDays(-_quoteHistoryDays);
        var dailyTrackers = await _dataStore.GetDailyTrackersAsync();
        foreach (var tracker in dailyTrackers)
        {
            await _dataStore.RemoveDailyQuotesAsync(tracker.Symbol, threshold);
        }
        return true;
    }
}
