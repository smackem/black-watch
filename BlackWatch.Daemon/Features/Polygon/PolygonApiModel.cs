using System.Collections.Generic;
using System.Text.Json.Serialization;
// ReSharper disable ClassNeverInstantiated.Global

namespace BlackWatch.Daemon.Features.Polygon
{
    public record GroupedDailyCryptoPricesResponse(
        string Status,
        string RequestId,
        int Count,
        IEnumerable<GroupedDailyCryptoPriceResult> Results);

    public record GroupedDailyCryptoPriceResult(
        [property: JsonPropertyName("T")] string Symbol,
        [property: JsonPropertyName("v")] decimal Volume,
        [property: JsonPropertyName("o")] decimal Open,
        [property: JsonPropertyName("c")] decimal Close,
        [property: JsonPropertyName("h")] decimal High,
        [property: JsonPropertyName("t")] long Timestamp);
}
