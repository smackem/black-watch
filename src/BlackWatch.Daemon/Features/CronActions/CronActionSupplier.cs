using System.Collections.Generic;
using BlackWatch.Core.Contracts;
using BlackWatch.Core.Services;
using BlackWatch.Daemon.Cron;
using Cronos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BlackWatch.Daemon.Features.CronActions;

public class CronActionSupplier : ICronActionSupplier
{
    private readonly IDataStore _dataStore;
    private readonly ILogger<CronActionSupplier> _logger;
    private readonly SchedulerOptions _options;
    private readonly TallyService _tallyService;

    public CronActionSupplier(
        IDataStore dataStore,
        IOptions<SchedulerOptions> options,
        TallyService tallyService,
        ILogger<CronActionSupplier> logger)
    {
        _dataStore = dataStore;
        _tallyService = tallyService;
        _logger = logger;
        _options = options.Value;
    }

    public IEnumerable<CronAction> Actions
    {
        get
        {
            var historyDownloader = new QuoteHistoryRequestAction(
                CronExpression.Parse(_options.Cron.DownloadQuoteHistory),
                _dataStore,
                _logger,
                _options.QuoteHistoryDays);
            var snapshotDownloader = new QuoteSnapshotRequestAction(
                CronExpression.Parse(_options.Cron.DownloadQuoteSnapshot),
                _dataStore,
                _logger);
            var initializer = new InitializeCronAction(
                CronExpression.Parse("@every_minute"),
                _dataStore,
                _logger,
                historyDownloader);
            var cleaner = new CleanupCronAction(
                CronExpression.Parse(_options.Cron.Cleanup),
                _dataStore,
                _logger,
                _options.QuoteHistoryDays);

            return new CronAction[]
            {
                cleaner,
                historyDownloader,
                snapshotDownloader,
                initializer,
                CreateEvaluationAction(_options.Cron.EvaluationEveryHour, EvaluationInterval.OneHour),
                CreateEvaluationAction(_options.Cron.EvaluationEverySixHours, EvaluationInterval.SixHours),
                CreateEvaluationAction(_options.Cron.EvaluationEveryDay, EvaluationInterval.OneDay)
            };
        }
    }

    private EvaluationAction CreateEvaluationAction(string cronStr, EvaluationInterval interval)
    {
        return new EvaluationAction(CronExpression.Parse(cronStr), interval, _tallyService, _dataStore, _logger);
    }
}
