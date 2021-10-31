using System;
using System.Threading.Tasks;

namespace BlackWatch.Daemon.Features.Polygon
{
    public interface IPolygonApiClient
    {
        Task<GroupedDailyCryptoPricesResponse> GetGroupedDailyCryptoPrices(DateTimeOffset date);
    }
}
