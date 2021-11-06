using System.Collections.Generic;
using System.Text.Json.Serialization;
// ReSharper disable ClassNeverInstantiated.Global

namespace BlackWatch.Daemon.Features.Polygon
{
    public record GroupedDailyCurrencyPricesResponse(
        string Status,
        string RequestId,
        int Count,
        IReadOnlyCollection<GroupedDailyCurrencyPriceResult> Results);

    public record GroupedDailyCurrencyPriceResult(
        [property: JsonPropertyName("T")] string Symbol,
        [property: JsonPropertyName("v")] decimal Volume,
        [property: JsonPropertyName("o")] decimal Open,
        [property: JsonPropertyName("c")] decimal Close,
        [property: JsonPropertyName("h")] decimal High,
        [property: JsonPropertyName("t")] long Timestamp);

    public record AggregateCurrencyPricesResponse(
        string Status,
        string RequestId,
        int Count,
        string Ticker,
        IReadOnlyCollection<AggregateCurrencyPriceResult> Results);

    public record AggregateCurrencyPriceResult(
        [property: JsonPropertyName("v")] decimal Volume,
        [property: JsonPropertyName("o")] decimal Open,
        [property: JsonPropertyName("c")] decimal Close,
        [property: JsonPropertyName("h")] decimal High,
        [property: JsonPropertyName("l")] decimal Low,
        [property: JsonPropertyName("t")] long Timestamp,
        [property: JsonPropertyName("n")] long TransactionCount);
}
