using System;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Daemon.Cron;
using Cronos;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Daemon.Features.CronActions;

public class CleanupCronAction : CronAction
{
    private readonly IQuoteStore _quoteStore;
    private readonly ILogger _logger;
    private readonly int _quoteHistoryDays;

    public CleanupCronAction(
        CronExpression cronExpr,
        IQuoteStore quoteStore,
        ILogger logger,
        int quoteHistoryDays)
        : base(cronExpr, "cleanup")
    {
        _quoteStore = quoteStore;
        _logger = logger;
        _quoteHistoryDays = quoteHistoryDays;
    }

    public override async Task<bool> ExecuteAsync()
    {
        var threshold = DateTimeOffset.UtcNow.AddDays(-_quoteHistoryDays);
        var dailyTrackers = await _quoteStore.GetDailyTrackersAsync();
        _logger.LogInformation("cleanup cron action executing");
        foreach (var tracker in dailyTrackers)
        {
            await _quoteStore.RemoveDailyQuotesAsync(tracker.Symbol, threshold);
        }
        return true;
    }
}
