using System.Collections.Generic;
using BlackWatch.Core.Contracts;
using BlackWatch.Daemon.Cron;
using Cronos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BlackWatch.Daemon.Features.CronActions
{
    public class CronActionSupplier : ICronActionSupplier
    {
        private readonly IDataStore _dataStore;
        private readonly ILogger<CronActionSupplier> _logger;
        private readonly SchedulerOptions _options;

        public CronActionSupplier(
            IDataStore dataStore,
            IOptions<SchedulerOptions> options,
            ILogger<CronActionSupplier> logger)
        {
            _dataStore = dataStore;
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
                    initializer
                };
            }
        }
    }
}
