using System;
using System.Threading.Tasks;

namespace BlackWatch.Daemon.Features.Polygon
{
    public interface IPolygonApiClient
    {
        Task<GroupedDailyCurrencyPricesResponse> GetGroupedDailyCryptoPricesAsync(DateTimeOffset date);
        Task<AggregateCurrencyPricesResponse> GetAggregateCryptoPricesAsync(string symbol, DateTimeOffset fromDate, DateTimeOffset toDate);
    }
}
