using System;
using System.Text.Json;
using BlackWatch.Core.Contracts;
using BlackWatch.Core.Test.Util;
using BlackWatch.Core.Util;
using Xunit;
using Xunit.Abstractions;

namespace BlackWatch.Core.Test
{
    public class JobInfoTest
    {
        private readonly ITestOutputHelper _out;

        public JobInfoTest(ITestOutputHelper @out)
        {
            _out = @out;
        }

        [Fact]
        public void SerializeAggregateCrypto()
        {
            var (from, to) = DateRange.DaysUntilYesterdayUtc(1);
            var jobInfo = JobInfo.DownloadQuoteHistory(new QuoteHistoryDownloadJob("SYM", from, to));
            var json = JsonSerializer.Serialize(jobInfo, new JsonSerializerOptions { IgnoreNullValues = true });
            _out.WriteLine($"serialized JobInfo: {json}");
            var deserialized = JsonSerializer.Deserialize<JobInfo>(json);
            Assert.NotNull(deserialized);
            Assert.Null(deserialized!.TrackerDownload);
            Assert.NotNull(deserialized.QuoteHistoryDownload);
            Assert.Equal(jobInfo.QuoteHistoryDownload, deserialized.QuoteHistoryDownload);
        }
 
        [Fact]
        public void SerializeDailyGroupedCrypto()
        {
            var jobInfo = JobInfo.DownloadTrackers(new TrackerDownloadJob(DateTimeOffset.Now));
            var json = JsonSerializer.Serialize(jobInfo, new JsonSerializerOptions { IgnoreNullValues = true });
            _out.WriteLine($"serialized JobInfo: {json}");
            var deserialized = JsonSerializer.Deserialize<JobInfo>(json);
            Assert.NotNull(deserialized);
            Assert.Null(deserialized!.QuoteHistoryDownload);
            Assert.NotNull(deserialized.TrackerDownload);
            Assert.Equal(jobInfo.TrackerDownload, deserialized.TrackerDownload);
        }
    }
}