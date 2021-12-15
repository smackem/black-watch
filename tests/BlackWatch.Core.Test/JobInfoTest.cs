using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using BlackWatch.Core.Contracts;
using BlackWatch.Core.Util;
using Xunit;
using Xunit.Abstractions;

namespace BlackWatch.Core.Test;

public class JobInfoTest
{
    private readonly ITestOutputHelper _out;

    public JobInfoTest(ITestOutputHelper @out)
    {
        _out = @out;
    }

    [Fact] public void SerializeAggregateCrypto()
    {
        var (from, to) = DateRange.DaysUntilYesterdayUtc(1);
        var jobInfo = RequestInfo.DownloadQuoteHistory(new QuoteHistoryRequestInfo("SYM", from, to), "some_api");
        var json = JsonSerializer.Serialize(jobInfo, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        });
        _out.WriteLine($"serialized JobInfo: {json}");
        var deserialized = JsonSerializer.Deserialize<RequestInfo>(json);
        Assert.NotNull(deserialized);
        Assert.Null(deserialized!.TrackerDownload);
        Assert.NotNull(deserialized.QuoteHistoryDownload);
        Assert.Equal(jobInfo.QuoteHistoryDownload, deserialized.QuoteHistoryDownload);
    }

    [Fact] public void SerializeDailyGroupedCrypto()
    {
        var jobInfo = RequestInfo.DownloadTrackers(new TrackerRequestInfo(DateTimeOffset.Now, 1), "some_api");
        var json = JsonSerializer.Serialize(jobInfo, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        });
        _out.WriteLine($"serialized JobInfo: {json}");
        var deserialized = JsonSerializer.Deserialize<RequestInfo>(json);
        Assert.NotNull(deserialized);
        Assert.Null(deserialized!.QuoteHistoryDownload);
        Assert.NotNull(deserialized.TrackerDownload);
        Assert.Equal(jobInfo.TrackerDownload, deserialized.TrackerDownload);
    }
}
