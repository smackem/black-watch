using System;
using System.Threading.Tasks;

namespace BlackWatch.Daemon.Features.PolygonApi;

public interface IPolygonApiClient
{
    /// <summary>
    ///     gets the crypto prices (OHLC) of all trackers provided by the polygon API
    /// </summary>
    Task<GroupedDailyCurrencyPricesResponse> GetGroupedDailyCryptoPricesAsync(DateTimeOffset date);

    /// <summary>
    ///     gets the day-by-day OHLC in the given timeframe for the given symbol
    /// </summary>
    Task<AggregateCurrencyPricesResponse> GetAggregateCryptoPricesAsync(string symbol, DateTimeOffset fromDate, DateTimeOffset toDate);
}
