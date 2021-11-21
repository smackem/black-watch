using System;

namespace BlackWatch.Core.Contracts
{
    /// <summary>
    /// symbol tracker as stored in the <see cref="IDataStore"/>
    /// </summary>
    /// <param name="Symbol"></param>
    /// <param name="OldestQuote">the date of the oldest quote stored in the db</param>
    /// <param name="LatestQuote">the date of latest quote stored in the db</param>
    public record Tracker(
        string Symbol,
        DateTimeOffset? OldestQuote,
        DateTimeOffset? LatestQuote);
}
