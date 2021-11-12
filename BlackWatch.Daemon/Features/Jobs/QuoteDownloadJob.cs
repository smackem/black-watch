using System;
using System.Linq;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Daemon.Features.Polygon;
using BlackWatch.Daemon.JobEngine;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Daemon.Features.Jobs
{
    public class QuoteDownloadJob : Job
    {
        private readonly QuoteHistoryDownloadJob _info;
        private readonly IPolygonApiClient _polygon;
        private readonly IDataStore _dataStore;

        public QuoteDownloadJob(QuoteHistoryDownloadJob info, IDataStore dataStore, IPolygonApiClient polygon)
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
                    DateTimeOffset.FromUnixTimeMilliseconds(p.Timestamp)))
                .ToArray();

            foreach (var quote in quotes)
            {
                await _dataStore.SetQuoteAsync(quote);
            }

            await UpdateTracker(quotes);

            return JobExecutionResult.Ok;
        }

        private static string GetCryptoQuoteCurrency(string symbol)
        {
            return symbol.Length > 3 ? symbol[^3..] : string.Empty;
        }

        private Task UpdateTracker(Quote[] quotes)
        {
            var (from, to) = GetTrackerDateRange(quotes);
            var tracker = new Tracker(_info.Symbol, from, to);
            return _dataStore.InsertTrackersAsync(new[] { tracker });
        }

        private (DateTimeOffset? from, DateTimeOffset? to) GetTrackerDateRange(Quote[] quotes)
        {
            DateTimeOffset? from = null;
            DateTimeOffset? to = null;

            foreach (var quote in quotes)
            {
                if (from == null || quote.Date < from)
                {
                    from = quote.Date;
                }

                if (to == null || quote.Date > to)
                {
                    to = quote.Date;
                }
            }

            return (from, to);
        }
    }
}
