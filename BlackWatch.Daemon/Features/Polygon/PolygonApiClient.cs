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

        public PolygonApiClient(HttpClient http, IConfiguration configuration, ILogger<PolygonApiClient> logger)
        {
            _http = http;
            _apiKey = configuration["Polygon:ApiKey"];
            _logger = logger;
        }

        public async Task<GroupedDailyCryptoPricesResponse> GetGroupedDailyCryptoPrices(DateTimeOffset date)
        {
            var path = $"aggs/grouped/locale/global/market/crypto/{date:yyyy-MM-dd}?apiKey={_apiKey}";
            var response = await _http.GetFromJsonAsync<GroupedDailyCryptoPricesResponse>(path,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
            return response!;
        }
    }
}
