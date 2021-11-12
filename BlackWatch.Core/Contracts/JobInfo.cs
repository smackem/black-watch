using System;
using System.Text.Json.Serialization;

namespace BlackWatch.Core.Contracts
{
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

    public record TrackerDownloadJob(DateTimeOffset Date);

    public record QuoteHistoryDownloadJob(string Symbol, DateTimeOffset FromDate, DateTimeOffset ToDate);
}