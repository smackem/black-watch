using System;

namespace BlackWatch.Core.Util
{
    public static class DateRange
    {
        public static (DateTimeOffset from, DateTimeOffset to) DaysUntilToday(int days)
        {
            var now = DateTimeOffset.Now;
            return (now.AddDays(-days), now);
        }
    }
}