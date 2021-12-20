using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Daemon.Features.PolygonApi;
using BlackWatch.Daemon.RequestEngine;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Daemon.Features.Requests.Polygon;

internal class QuoteHistoryRequest : Request
{
    private readonly IQuoteStore _quoteStore;
    private readonly QuoteHistoryRequestInfo _info;
    private readonly IPolygonApiClient _polygon;

    public QuoteHistoryRequest(QuoteHistoryRequestInfo info, IQuoteStore quoteStore, IPolygonApiClient polygon)
        : base($"download aggregates for {info.Symbol}")
    {
        _info = info;
        _polygon = polygon;
        _quoteStore = quoteStore;
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

        var currency = PolygonNaming.ExtractCurrency(_info.Symbol);
        var symbol = PolygonNaming.AdjustSymbol(_info.Symbol);
        var quotes = prices.Results
            .Select(p => new Quote(
                symbol, p.Open, p.Close, p.High, 0, currency,
                DateTimeOffset.FromUnixTimeMilliseconds(p.Timestamp)))
            .ToArray();

        foreach (var quote in quotes)
        {
            await _quoteStore.PutDailyQuoteAsync(quote);
        }

        return RequestResult.Ok;
    }

    private static (DateTimeOffset? from, DateTimeOffset? to) GetTrackerDateRange(IEnumerable<Quote> quotes)
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
