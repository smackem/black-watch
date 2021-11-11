using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Daemon.Features.Polygon;
using BlackWatch.Daemon.JobEngine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BlackWatch.Daemon
{
    public class JobExecutor : WorkerBase
    {
        private readonly ILogger<JobExecutor> _logger;
        private readonly IDataStore _dataStore;
        private readonly IPolygonApiClient _polygon;
        private readonly JobExecutorOptions _config;

        public JobExecutor(ILogger<JobExecutor> logger, IDataStore dataStore, IPolygonApiClient polygon,
            IOptions<JobExecutorOptions> options)
        : base(logger)
        {
            _logger = logger;
            _dataStore = dataStore;
            _polygon = polygon;
            _config = options.Value;
        }

        protected override async Task ExecuteOverrideAsync(CancellationToken stoppingToken)
        {
            while (stoppingToken.IsCancellationRequested == false)
            {
                var jobInfos = await _dataStore.DequeueJobsAsync(_config.MaxJobsPerMinute);
                var jobs = jobInfos.Select(info => (BuildJob(info), info));
                var ctx = new JobExecutionContext(_logger)
                {
                    StoppingToken = stoppingToken,
                };

                foreach (var (job, jobInfo) in jobs)
                {
                    var result = await job.ExecuteAsync(ctx);

                    _logger.Log(GetLogLevel(result), "job {Job} executed with result: {JobExecutionResult}", job, result);

                    if (result == JobExecutionResult.Retry)
                    {
                        await _dataStore.EnqueueJobAsync(jobInfo);
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }

            _logger.LogInformation("job execution stopped");
        }

        private static LogLevel GetLogLevel(JobExecutionResult result)
        {
            return result switch
            {
                JobExecutionResult.Ok => LogLevel.Information,
                JobExecutionResult.Retry => LogLevel.Warning,
                JobExecutionResult.Fatal => LogLevel.Error,
                _ => throw new ArgumentOutOfRangeException($"unknown execution result: {result}"),
            };
        }

        private Job BuildJob(JobInfo jobInfo)
        {
            return jobInfo switch
            {
                { AggregateCrypto: not null } => new QuoteDownloadJob(jobInfo.AggregateCrypto, _dataStore, _polygon),
                { DailyGroupedCrypto: not null } => new TrackerDownloadJob(jobInfo.DailyGroupedCrypto, _dataStore, _polygon),
                var info when info == JobInfo.Nop => NopJob.Instance,
                _ => throw new ArgumentException($"unkown kind of job: {jobInfo}"),
            };
        }

        private class QuoteDownloadJob : Job
        {
            private readonly AggregateCryptoJob _info;
            private readonly IPolygonApiClient _polygon;
            private readonly IDataStore _dataStore;

            public QuoteDownloadJob(AggregateCryptoJob info, IDataStore dataStore, IPolygonApiClient polygon)
                : base($"download aggregates for {info.Symbol}")
            {
                _info = info;
                _polygon = polygon;
                _dataStore = dataStore;
            }

            public override async Task<JobExecutionResult> ExecuteAsync(JobExecutionContext ctx)
            {
                AggregateCurrencyPricesResponse prices;
                try
                {
                    prices = await _polygon.GetAggregateCryptoPricesAsync(_info.Symbol, _info.FromDate, _info.ToDate);
                }
                catch (Exception e)
                {
                    ctx.Logger.LogError(e, "error getting aggregate crypto prices for {Symbol}", _info.Symbol);
                    return JobExecutionResult.Fatal;
                }

                if (prices.Status != PolygonApiStatus.Ok)
                {
                    ctx.Logger.LogWarning("aggregate crypto prices: got non-ok status: {Response}", prices);
                }

                if (prices.Results == null)
                {
                    ctx.Logger.LogWarning("aggregate crypto prices: got empty result set: {Response}", prices);
                    return JobExecutionResult.Retry;
                }

                var currency = GetCryptoQuoteCurrency(_info.Symbol);
                var quotes = prices.Results
                    .Select(p => new Quote(
                        _info.Symbol, p.Open, p.Close, p.High, 0, currency,
                        DateTimeOffset.FromUnixTimeMilliseconds(p.Timestamp)));

                foreach (var quote in quotes)
                {
                    await _dataStore.SetQuoteAsync(quote);
                }

                return JobExecutionResult.Ok;
            }

            private static string GetCryptoQuoteCurrency(string symbol)
            {
                return symbol.Length > 3 ? symbol[^3..] : string.Empty;
            }
        }

        private class TrackerDownloadJob : Job
        {
            private readonly DailyGroupedCryptoJob _info;
            private readonly IPolygonApiClient _polygon;
            private readonly IDataStore _dataStore;

            public TrackerDownloadJob(DailyGroupedCryptoJob info, IDataStore dataStore, IPolygonApiClient polygon)
                : base("download crypto trackers")
            {
                _info = info;
                _dataStore = dataStore;
                _polygon = polygon;
            }

            public override async Task<JobExecutionResult> ExecuteAsync(JobExecutionContext ctx)
            {
                GroupedDailyCurrencyPricesResponse trackerPrices;
                try
                {
                    trackerPrices = await _polygon.GetGroupedDailyCryptoPricesAsync(_info.Date);
                }
                catch (Exception e)
                {
                    ctx.Logger.LogError(e, "error getting grouped daily crypto prices");
                    return JobExecutionResult.Fatal;
                }

                ctx.Logger.LogDebug("{Response}", trackerPrices);

                if (trackerPrices.Status != PolygonApiStatus.Ok)
                {
                    ctx.Logger.LogWarning("aggregate crypto prices: got non-ok status: {Response}", trackerPrices);
                }

                if (trackerPrices.Results == null)
                {
                    ctx.Logger.LogWarning("grouped daily crypto prices: got empty result set: {Response}", trackerPrices);
                    return JobExecutionResult.Retry;
                }

                var trackers = trackerPrices.Results
                    .Select(tp => new Tracker(tp.Symbol, null, null))
                    .ToArray();

                await _dataStore.InsertTrackersAsync(trackers);
                return JobExecutionResult.Ok;
            }
        }
    }
}
