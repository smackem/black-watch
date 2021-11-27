using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Core.Services;
using BlackWatch.Core.Util;
using BlackWatch.Daemon.Cron;
using Cronos;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Daemon.Features.CronActions
{
    public class HourlyEvalAction : CronAction
    {
        private readonly TallyService _tallyService;
        private readonly IDataStore _dataStore;
        private readonly ILogger _logger;

        public HourlyEvalAction(
            CronExpression cronExpr,
            TallyService tallyService,
            IDataStore dataStore,
            ILogger logger)
            : base(cronExpr, "evaluate hourly tally sources")
        {
            _tallyService = tallyService;
            _dataStore = dataStore;
            _logger = logger;
        }

        public override async Task<bool> ExecuteAsync()
        {
            await foreach (var tally in _tallyService.EvaluateAsync(EvaluationInterval.OneHour).Linger())
            {
                await _dataStore.PutTallyAsync(tally);
            }

            return true;
        }
    }
}
