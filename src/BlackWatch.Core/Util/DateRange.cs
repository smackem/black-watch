using System;

namespace BlackWatch.Core.Util;

public static class DateRange
{
    public static (DateTimeOffset from, DateTimeOffset to) DaysUntilYesterdayUtc(int days)
    {
        var yesterday = DateTimeOffset.UtcNow.AddDays(-1);
        return (yesterday.AddDays(-days), yesterday);
    }
}