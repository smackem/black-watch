using System;
using System.Text.Json.Serialization;

namespace BlackWatch.Core.Contracts
{
    public record JobInfo
    {
        [JsonInclude]
        public DailyGroupedCryptoJob? DailyGroupedCrypto { get; private init; }

        [JsonInclude]
        public AggregateCryptoJob? AggregateCrypto { get; private init; }

        public static readonly JobInfo Nop = new();

        public static JobInfo GetDailyGroupedCrypto(DailyGroupedCryptoJob job)
        {
            return new JobInfo { DailyGroupedCrypto = job };
        }

        public static JobInfo GetAggregateCrypto(AggregateCryptoJob job)
        {
            return new JobInfo { AggregateCrypto = job };
        }
    }

    public record DailyGroupedCryptoJob(DateTimeOffset Date);

    public record AggregateCryptoJob(string Symbol, DateTimeOffset FromDate, DateTimeOffset ToDate);
}