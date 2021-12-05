using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Daemon.Features.PolygonApi;
using BlackWatch.Daemon.RequestEngine;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Daemon.Features.Requests.Polygon
{
    internal class QuoteHistoryRequest : Request
    {
        private readonly QuoteHistoryRequestInfo _info;
        private readonly IPolygonApiClient _polygon;
        private readonly IDataStore _dataStore;

        public QuoteHistoryRequest(QuoteHistoryRequestInfo info, IDataStore dataStore, IPolygonApiClient polygon)
            : base($"download aggregates for {info.Symbol}")
        {
            _info = info;
            _polygon = polygon;
            _dataStore = dataStore;
        }

        public override async Task<RequestResult> ExecuteAsync(RequestContext ctx)
        {
            AggregateCurrencyPricesResponse prices;
            try
            {
                prices = await _polygon.GetAggregateCryptoPricesAsync(_info.Symbol, _info.FromDate, _info.ToDate);
            }
            catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.TooManyRequests)
            {
                ctx.Logger.LogWarning(e, "received {StatusCode} while getting aggregate crypto prices for {Symbol} => wait and retry", e.StatusCode, _info.Symbol);
                return RequestResult.WaitAndRetry;
            }
            catch (Exception e)
            {
                ctx.Logger.LogError(e, "error getting aggregate crypto prices for {Symbol}", _info.Symbol);
                return RequestResult.Fatal;
            }

            if (prices.Status != PolygonApiStatus.Ok)
            {
                ctx.Logger.LogWarning("aggregate crypto prices: got non-ok status: {Response}", prices);
            }

            if (prices.Results == null)
            {
                ctx.Logger.LogWarning("aggregate crypto prices: got empty result set: {Response}", prices);
                return RequestResult.Retry;
            }

            var currency = GetCryptoQuoteCurrency(_info.Symbol);
            var quotes = prices.Results
                .Select(p => new Quote(
                    _info.Symbol, p.Open, p.Close, p.High, 0, currency,
                    DateTimeOffset.FromUnixTimeMilliseconds(p.Timestamp)))
                .ToArray();

            foreach (var quote in quotes)
            {
                await _dataStore.PutDailyQuoteAsync(quote);
            }

            await UpdateTracker(quotes);
            return RequestResult.Ok;
        }

        private static string GetCryptoQuoteCurrency(string symbol)
        {
            return symbol.Length > 3 ? symbol[^3..] : string.Empty;
        }

        private Task UpdateTracker(Quote[] quotes)
        {
            var (from, to) = GetTrackerDateRange(quotes);
            var tracker = new Tracker(_info.Symbol, from, to);
            return _dataStore.PutTrackersAsync(new[] { tracker });
        }

        private static (DateTimeOffset? from, DateTimeOffset? to) GetTrackerDateRange(Quote[] quotes)
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
