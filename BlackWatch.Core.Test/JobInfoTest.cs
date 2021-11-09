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
            var (from, to) = DateRange.DaysUntilToday(1);
            var jobInfo = JobInfo.GetAggregateCrypto(new AggregateCryptoJob("SYM", from, to));
            var json = JsonSerializer.Serialize(jobInfo, new JsonSerializerOptions { IgnoreNullValues = true });
            _out.WriteLine($"serialized JobInfo: {json}");
            var deserialized = JsonSerializer.Deserialize<JobInfo>(json);
            Assert.NotNull(deserialized);
            Assert.Null(deserialized.DailyGroupedCrypto);
            Assert.NotNull(deserialized.AggregateCrypto);
            Assert.Equal(jobInfo.AggregateCrypto, deserialized.AggregateCrypto);
        }
 
        [Fact]
        public void SerializeDailyGroupedCrypto()
        {
            var jobInfo = JobInfo.GetDailyGroupedCrypto(new DailyGroupedCryptoJob(DateTimeOffset.Now));
            var json = JsonSerializer.Serialize(jobInfo, new JsonSerializerOptions { IgnoreNullValues = true });
            _out.WriteLine($"serialized JobInfo: {json}");
            var deserialized = JsonSerializer.Deserialize<JobInfo>(json);
            Assert.NotNull(deserialized);
            Assert.Null(deserialized.AggregateCrypto);
            Assert.NotNull(deserialized.DailyGroupedCrypto);
            Assert.Equal(jobInfo.DailyGroupedCrypto, deserialized.DailyGroupedCrypto);
        }
    }
}