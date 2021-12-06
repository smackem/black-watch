using System;
using System.Text.Json.Serialization;

namespace BlackWatch.Core.Contracts
{
    /// <summary>
    /// a json-serializable discriminated union of different kinds of jobs
    /// </summary>
    public record RequestInfo
    {
        [JsonInclude]
        public TrackerRequestInfo? TrackerDownload { get; private init; }

        [JsonInclude]
        public QuoteHistoryRequestInfo? QuoteHistoryDownload { get; private init; }
        
        [JsonInclude]
        public QuoteSnapshotRequestInfo? QuoteSnapshotDownload { get; private init; }

        [JsonInclude]
        public string? ApiTag { get; private init; }

        public static readonly RequestInfo Nop = new();

        public static RequestInfo DownloadTrackers(TrackerRequestInfo requestInfo, string apiTag)
        {
            return new RequestInfo { TrackerDownload = requestInfo, ApiTag = apiTag };
        }

        public static RequestInfo DownloadQuoteHistory(QuoteHistoryRequestInfo requestInfo, string apiTag)
        {
            return new RequestInfo { QuoteHistoryDownload = requestInfo, ApiTag = apiTag };
        }

        public static RequestInfo DownloadQuoteSnapshots(string apiTag)
        {
            return new RequestInfo { QuoteSnapshotDownload = new QuoteSnapshotRequestInfo(), ApiTag = apiTag };
        }
    }

    /// <summary>
    /// download all trackers including their market infos at <paramref name="Date"/>, then queue history requests
    /// for <paramref name="QuoteHistoryDays"/> for each tracker
    /// </summary>
    public record TrackerRequestInfo(DateTimeOffset Date, int QuoteHistoryDays);

    /// <summary>
    /// download the daily quotes for the tracker with symbol <paramref name="Symbol"/>, starting
    /// at <paramref name="FromDate"/> up to and including <paramref name="ToDate"/>
    /// </summary>
    public record QuoteHistoryRequestInfo(string Symbol, DateTimeOffset FromDate, DateTimeOffset ToDate);

    /// <summary>
    /// download a snapshot (current price &amp; hourly ohlcv) of all available trackers
    /// </summary>
    public record QuoteSnapshotRequestInfo;
}