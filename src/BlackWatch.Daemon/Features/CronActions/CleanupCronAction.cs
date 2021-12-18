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

    public CleanupCronAction(
        CronExpression cronExpr,
        IDataStore dataStore,
        ILogger logger)
        : base(cronExpr, "cleanup")
    {
        _dataStore = dataStore;
        _logger = logger;
    }

    public override async Task<bool> ExecuteAsync()
    {
        var dailyTrackers = await _dataStore.GetDailyTrackersAsync();
        throw new NotImplementedException();
    }
}
