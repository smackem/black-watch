using System;
using System.Text.Json.Serialization;

namespace BlackWatch.Core.Contracts
{
    /// <summary>
    /// a json-serializable discriminated union of different kinds of jobs
    /// </summary>
    public record JobInfo
    {
        [JsonInclude]
        public TrackerDownloadJob? TrackerDownload { get; private init; }

        [JsonInclude]
        public QuoteHistoryDownloadJob? QuoteHistoryDownload { get; private init; }

        public static readonly JobInfo Nop = new();

        public static JobInfo DownloadTrackers(TrackerDownloadJob job)
        {
            return new JobInfo { TrackerDownload = job };
        }

        public static JobInfo DownloadQuoteHistory(QuoteHistoryDownloadJob job)
        {
            return new JobInfo { QuoteHistoryDownload = job };
        }
    }

    /// <summary>
    /// download all trackers including their market infos at <paramref name="Date"/>
    /// </summary>
    public record TrackerDownloadJob(DateTimeOffset Date);

    /// <summary>
    /// download the daily quotes for the tracker with symbol <paramref name="Symbol"/>, starting
    /// at <paramref name="FromDate"/> up to and including <paramref name="ToDate"/>
    /// </summary>
    public record QuoteHistoryDownloadJob(string Symbol, DateTimeOffset FromDate, DateTimeOffset ToDate);
}