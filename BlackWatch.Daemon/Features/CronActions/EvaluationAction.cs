using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Core.Services;
using BlackWatch.Core.Util;
using BlackWatch.Daemon.Cron;
using Cronos;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Daemon.Features.CronActions
{
    public class EvaluationAction : CronAction
    {
        private readonly EvaluationInterval _interval;
        private readonly TallyService _tallyService;
        private readonly IDataStore _dataStore;
        private readonly ILogger _logger;

        public EvaluationAction(
            CronExpression cronExpr,
            EvaluationInterval interval,
            TallyService tallyService,
            IDataStore dataStore,
            ILogger logger)
            : base(cronExpr, "evaluate hourly tally sources")
        {
            _interval = interval;
            _tallyService = tallyService;
            _dataStore = dataStore;
            _logger = logger;
        }

        public override async Task<bool> ExecuteAsync()
        {
            var tallies = _tallyService.EvaluateAsync(_interval);
            var count = 0;
            _logger.LogDebug("evaluating tally sources with interval {Interval}", _interval);

            await foreach (var tally in tallies.Linger())
            {
                await _dataStore.PutTallyAsync(tally);
                count++;
            }

            _logger.LogInformation("evaluating tally sources with interval {Interval} yielded {TallyCount} tallies", _interval, count);
            return true;
        }
    }
}
