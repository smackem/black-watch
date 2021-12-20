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
    private readonly IRequestQueue _requestQueue;
    private readonly IQuoteStore _quoteStore;
    private readonly IUserDataStore _userDataStore;
    private readonly ILogger<CronActionSupplier> _logger;
    private readonly SchedulerOptions _options;
    private readonly TallyService _tallyService;

    public CronActionSupplier(
        IRequestQueue requestQueue,
        IQuoteStore quoteStore,
        IUserDataStore userDataStore,
        IOptions<SchedulerOptions> options,
        TallyService tallyService,
        ILogger<CronActionSupplier> logger)
    {
        _requestQueue = requestQueue;
        _quoteStore = quoteStore;
        _userDataStore = userDataStore;
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
                _requestQueue,
                _logger,
                _options.QuoteHistoryDays);
            var snapshotDownloader = new QuoteSnapshotRequestAction(
                CronExpression.Parse(_options.Cron.DownloadQuoteSnapshot),
                _requestQueue,
                _logger);
            var initializer = new InitializeCronAction(
                CronExpression.Parse("@every_minute"),
                _requestQueue,
                _quoteStore,
                _logger,
                historyDownloader);
            var cleaner = new CleanupCronAction(
                CronExpression.Parse(_options.Cron.Cleanup),
                _quoteStore,
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
        return new EvaluationAction(CronExpression.Parse(cronStr), interval, _tallyService, _userDataStore, _logger);
    }
}
