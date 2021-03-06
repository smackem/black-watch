using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BlackWatch.Daemon.Features.MessariApi;

public class MessariApiClient : IMessariApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<MessariApiClient> _logger;
    private readonly MessariApiClientOptions _options;

    public MessariApiClient(HttpClient http, ILogger<MessariApiClient> logger, IOptions<MessariApiClientOptions> options)
    {
        _http = http;
        _logger = logger;
        _options = options.Value;

        _logger.LogDebug("{ClassName} created, base address = {BaseAddress}", nameof(MessariApiClient), http.BaseAddress);
    }

    public async Task<AssetListResponse> GetAssetsAsync(int page)
    {
        if (page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(page), "value must be positive");
        }

        // https://data.messari.io/api/v2/assets?fields=id,slug,symbol,metrics/market_data/price_usd,metrics/market_data/ohlcv_last_1_hour&limit=10
        var path = $"assets?fields=id,slug,symbol,metrics/market_data/price_usd,metrics/market_data/ohlcv_last_1_hour&limit={_options.QuoteLimit}&page={page}";
        var response = await _http.GetFromJsonAsync<AssetListResponse>(path);
        return response!;
    }
}
