using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Daemon.Features.Messari
{
    public class MessariApiClient : IMessariApiClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<MessariApiClient> _logger;

        public MessariApiClient(HttpClient http, ILogger<MessariApiClient> logger)
        {
            _http = http;
            _logger = logger;

            _logger.LogDebug("{ClassName} created, base address = {BaseAddress}", nameof(MessariApiClient), http.BaseAddress);
        }

        public async Task<AssetListResponse> GetAssetsAsync(int limit, int page)
        {
            if (limit is <= 0 or > 500)
            {
                throw new ArgumentOutOfRangeException(nameof(limit), "value must be between 1 and 500 inclusively");
            }
            if (page < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(page), "value must be positive");
            }

            // https://data.messari.io/api/v2/assets?fields=id,slug,symbol,metrics/market_data/price_usd,metrics/market_data/ohlcv_last_1_hour&limit=10
            var path = $"assets?fields=id,slug,symbol,metrics/market_data/price_usd,metrics/market_data/ohlcv_last_1_hour&limit={limit}&page={page}";
            var response = await _http.GetFromJsonAsync<AssetListResponse>(path);
            return response!;
        }
    }
}
