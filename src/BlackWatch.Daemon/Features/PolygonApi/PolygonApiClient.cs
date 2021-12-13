using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BlackWatch.Daemon.Features.PolygonApi;

public class PolygonApiClient : IPolygonApiClient
{
    private static readonly TimeSpan MaxAggregationRange = TimeSpan.FromDays(1000);
    private readonly HttpClient _http;
    private readonly ILogger<PolygonApiClient> _logger;
    private readonly PolygonApiClientOptions _options;

    public PolygonApiClient(HttpClient http, IOptions<PolygonApiClientOptions> options, ILogger<PolygonApiClient> logger)
    {
        if (http.BaseAddress != new Uri(options.Value.BaseAddress!))
        {
            throw new ArgumentException($"http client base address mismatch: {http.BaseAddress} <-> {options.Value.BaseAddress}");
        }

        _http = http;
        _logger = logger;
        _options = options.Value;

        _logger.LogDebug("{ClassName} created, base address = {BaseAddress}", nameof(PolygonApiClient), http.BaseAddress);
    }

    public async Task<GroupedDailyCurrencyPricesResponse> GetGroupedDailyCryptoPricesAsync(DateTimeOffset date)
    {
        var path = $"aggs/grouped/locale/global/market/crypto/{date:yyyy-MM-dd}?apiKey={_options.ApiKey}";
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

        var path = $"aggs/ticker/{symbol}/range/1/day/{fromDate:yyyy-MM-dd}/{toDate:yyyy-MM-dd}?sort=asc&limit=1000&apiKey={_options.ApiKey}";
        var response = await GetAsync<AggregateCurrencyPricesResponse>(path);
        return response!;
    }

    private Task<T?> GetAsync<T>(string path)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return _http.GetFromJsonAsync<T>(path, options);
    }
}
