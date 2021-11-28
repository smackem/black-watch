using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Daemon.Features.Polygon
{
    public class PolygonApiClient : IPolygonApiClient
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly ILogger<PolygonApiClient> _logger;
        private static readonly TimeSpan MaxAggregationRange = TimeSpan.FromDays(1000);

        public PolygonApiClient(HttpClient http, IConfiguration configuration, ILogger<PolygonApiClient> logger)
        {
            _http = http;
            _apiKey = configuration["Polygon:ApiKey"];
            _logger = logger;
        }

        public async Task<GroupedDailyCurrencyPricesResponse> GetGroupedDailyCryptoPricesAsync(DateTimeOffset date)
        {
            var path = $"aggs/grouped/locale/global/market/crypto/{date:yyyy-MM-dd}?apiKey={_apiKey}";
            var response = await GetAsync<GroupedDailyCurrencyPricesResponse>(path);
            return response!;
        }

        public async Task<AggregateCurrencyPricesResponse> GetAggregateCryptoPricesAsync(string symbol, DateTimeOffset fromDate, DateTimeOffset toDate)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                throw new ArgumentException("symbol must no be blank", nameof(symbol));
            }
            if (toDate < fromDate || toDate - fromDate > MaxAggregationRange)
            {
                throw new ArgumentException("invalid time range", nameof(toDate));
            }

            var path = $"aggs/ticker/{symbol}/range/1/day/{fromDate:yyyy-MM-dd}/{toDate:yyyy-MM-dd}?sort=asc&limit=1000&apiKey={_apiKey}";
            var response = await GetAsync<AggregateCurrencyPricesResponse>(path);
            return response!;
        }

        private Task<T?> GetAsync<T>(string path)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

            return _http.GetFromJsonAsync<T>(path, options);
        }
    }
}
