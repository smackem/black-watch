using System;
using BlackWatch.WebApp.Features.Api;

namespace BlackWatch.WebApp.Util;

public static class RenderExtensions
{
    public static string Render(this DateTimeOffset self)
    {
        var local = self.ToLocalTime().DateTime;
        var now = DateTime.Now;
        var datePart = local.Date switch
        {
            var d when d == now.Date => "Today",
            var d when d == now.Date.AddDays(-1).Date => "Yesterday",
            _ => local.ToShortDateString(),
        };
        return $"{datePart} {local.ToShortTimeString()}";
    }

    public static string Render(this EvaluationInterval self)
    {
        return self switch
        {
            EvaluationInterval.Disabled => "--",
            EvaluationInterval.OneHour => "1 hour",
            EvaluationInterval.SixHours => "6 hours",
            EvaluationInterval.OneDay => "1 day",
            _ => throw new ArgumentOutOfRangeException(nameof(self), self, null)
        };
    }
}
