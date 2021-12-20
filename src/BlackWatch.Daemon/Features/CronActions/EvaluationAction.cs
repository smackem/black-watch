using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Core.Services;
using BlackWatch.Core.Util;
using BlackWatch.Daemon.Cron;
using Cronos;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Daemon.Features.CronActions;

public class EvaluationAction : CronAction
{
    private readonly IUserDataStore _userDataStore;
    private readonly EvaluationInterval _interval;
    private readonly ILogger _logger;
    private readonly TallyService _tallyService;

    public EvaluationAction(
        CronExpression cronExpr,
        EvaluationInterval interval,
        TallyService tallyService,
        IUserDataStore userDataStore,
        ILogger logger)
        : base(cronExpr, $"evaluate tally sources @ interval {interval}")
    {
        _interval = interval;
        _tallyService = tallyService;
        _userDataStore = userDataStore;
        _logger = logger;
    }

    public override async Task<bool> ExecuteAsync()
    {
        var tallies = _tallyService.EvaluateAsync(_interval);
        var count = 0;
        _logger.LogDebug("evaluating tally sources at interval {Interval}", _interval);

        await foreach (var tally in tallies.Linger())
        {
            await _userDataStore.PutTallyAsync(tally);
            count++;
        }

        _logger.LogInformation("evaluating tally sources at interval {Interval} yielded {TallyCount} tallies", _interval, count);
        return true;
    }
}
