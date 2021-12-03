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
        public TrackersRequest? TrackerDownload { get; private init; }

        [JsonInclude]
        public QuoteHistoryRequest? QuoteHistoryDownload { get; private init; }

        public static readonly RequestInfo Nop = new();

        public static RequestInfo DownloadTrackers(TrackersRequest request)
        {
            return new RequestInfo { TrackerDownload = request };
        }

        public static RequestInfo DownloadQuoteHistory(QuoteHistoryRequest request)
        {
            return new RequestInfo { QuoteHistoryDownload = request };
        }
    }

    /// <summary>
    /// download all trackers including their market infos at <paramref name="Date"/>
    /// </summary>
    public record TrackersRequest(DateTimeOffset Date);

    /// <summary>
    /// download the daily quotes for the tracker with symbol <paramref name="Symbol"/>, starting
    /// at <paramref name="FromDate"/> up to and including <paramref name="ToDate"/>
    /// </summary>
    public record QuoteHistoryRequest(string Symbol, DateTimeOffset FromDate, DateTimeOffset ToDate);
}