using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlackWatch.Core.Util;
using BlackWatch.Daemon.Util;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Daemon.Cron
{
    public class CronActionRunner : WorkerBase
    {
        private readonly ILogger<CronActionRunner> _logger;
        private readonly ICronActionSupplier _actionSupplier;

        public CronActionRunner(ILogger<CronActionRunner> logger, ICronActionSupplier actionSupplier)
            : base(logger)
        {
            _logger = logger;
            _actionSupplier = actionSupplier;
        }

        protected override async Task ExecuteOverrideAsync(CancellationToken stoppingToken)
        {
            var tasks = _actionSupplier.Actions
                .Select(action => RunCronAction(action, stoppingToken));

            await Task.WhenAll(tasks);
        }

        private async Task RunCronAction(CronAction action, CancellationToken stoppingToken)
        {
            _logger.LogInformation("{CronActionMoniker}: scheduled for '{CronExpr}'", action.Moniker, action.CronExpr);

            while (stoppingToken.IsCancellationRequested == false)
            {
                var occurrence = action.CronExpr.GetNextOccurrence(DateTimeOffset.UtcNow, TimeZoneInfo.Utc);
                _logger.LogInformation("{CronActionMoniker}: next occurrence @ '{CronDate}'", action.Moniker, occurrence);

                if (occurrence == null)
                {
                    _logger.LogWarning("{CronActionMoniker}: no next occurrence => quit cron runner", action.Moniker);
                    break;
                }

                TimeSpan delay;
                while ((delay = occurrence.Value - DateTimeOffset.UtcNow) > TimeSpan.Zero)
                {
                    _logger.LogDebug("{CronActionMoniker}: waiting for {Delay} until execution", action.Moniker, delay);
                    await Task.Delay(delay, stoppingToken).Linger();
                }

                // ReSharper disable once InvertIf
                if (await action.ExecuteAsync().Linger() == false)
                {
                    _logger.LogInformation("{CronActionMoniker}: cron action signalled end", action.Moniker);
                    break;
                }
            }
        }
    }
}