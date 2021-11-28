using System;

namespace BlackWatch.Core.Util
{
    public static class DateRange
    {
        public static (DateTimeOffset from, DateTimeOffset to) DaysUntilYesterdayUtc(int days)
        {
            var now = DateTimeOffset.UtcNow.AddDays(-1);
            return (now.AddDays(-days), now);
        }
    }
}