using System;

namespace BlackWatch.Core.Contracts
{
    public record Tracker(
        string Symbol,
        DateTimeOffset? OldestQuote,
        DateTimeOffset? LatestQuote);
}
