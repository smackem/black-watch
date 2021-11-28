using System.Collections.Generic;
using BlackWatch.Core.Contracts;
using BlackWatch.Core.Services;
using BlackWatch.Daemon.Cron;
using Cronos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BlackWatch.Daemon.Features.CronActions
{
    public class CronActionSupplier : ICronActionSupplier
    {
        private readonly IDataStore _dataStore;
        private readonly TallyService _tallyService;
        private readonly ILogger<CronActionSupplier> _logger;
        private readonly SchedulerOptions _options;

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
                var quoteDownloader = new QuoteDownloadTriggerAction(
                    CronExpression.Parse(_options.Cron.DownloadQuoteHistory),
                    _dataStore,
                    _logger,
                    _options.QuoteHistoryDays);
                var trackerDownloader = new TrackerDownloadTriggerAction(
                    CronExpression.Parse(_options.Cron.DownloadTrackers),
                    _dataStore,
                    _logger);
                var initializer = new InitializeCronAction(
                    CronExpression.Parse("@every_minute"),
                    _dataStore,
                    _logger,
                    quoteDownloader,
                    trackerDownloader);

                return new CronAction[]
                {
                    quoteDownloader,
                    trackerDownloader,
                    initializer,
                    CreateEvaluationAction(_options.Cron.EvaluationEveryHour, EvaluationInterval.OneHour),
                    CreateEvaluationAction(_options.Cron.EvaluationEverySixHours, EvaluationInterval.SixHours),
                    CreateEvaluationAction(_options.Cron.EvaluationEveryDay, EvaluationInterval.OneDay),
                };
            }
        }

        private EvaluationAction CreateEvaluationAction(string cronStr, EvaluationInterval interval) =>
            new(CronExpression.Parse(cronStr), interval, _tallyService, _dataStore, _logger);
    }
}
