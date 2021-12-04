using System;
using System.Text.Json.Serialization;

namespace BlackWatch.Daemon.Features.Messari
{
    /*
    {
    "status": {
      "elapsed": 80,
      "timestamp": "2021-12-04T11:34:52.030405492Z"
    },
    "data": [
      {
        "id": "1e31218a-e44e-4285-820c-8282ee222035",
        "slug": "bitcoin",
        "symbol": "BTC",
        "metrics": {
          "market_data": {
            "price_usd": 46723.941082317535,
            "ohlcv_last_1_hour": {
              "open": 47395.128991213984,
              "high": 47570.69147604349,
              "low": 46602.87736223899,
              "close": 46722.22954226107,
              "volume": 504696361.2740229
            }
          }
        }
      },
    */

    public record AssetListResponse(
        StatusResult Status,
        AssetData Data);

    public record StatusResult(
        int Elapsed,
        DateTimeOffset Timestamp);

    public record AssetData(
        Guid Id,
        string Slug,
        string Symbol,
        AssetMetrics Metrics);

    public record AssetMetrics(
        [property: JsonPropertyName("market_data")] AssetMarketData MarketData);

    public record AssetMarketData(
        [property: JsonPropertyName("price_usd")] decimal PriceUsd,
        [property: JsonPropertyName("ohlcv_last_1_hour")] AssetOhlcv LastHour);

    public record AssetOhlcv(
        decimal Open,
        decimal High,
        decimal Low,
        decimal Close,
        decimal Volume);
}
