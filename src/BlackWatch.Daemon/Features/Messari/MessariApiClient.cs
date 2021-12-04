using System.Net.Http;
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
        }

        public Task<AssetListResponse> GetAssetsAsync(int limit, int page)
        {
            // https://data.messari.io/api/v2/assets?fields=id,slug,symbol,metrics/market_data/price_usd,metrics/market_data/ohlcv_last_1_hour&limit=10
            throw new System.NotImplementedException();
        }
    }
}
