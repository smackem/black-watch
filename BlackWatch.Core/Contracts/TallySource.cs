using System;

namespace BlackWatch.Core.Contracts
{
    public record TallySource(
        string Id,
        string Code,
        int Version,
        DateTimeOffset DateModified,
        EvaluationInterval Interval)
    {
        public TallySource Update(string code)
        {
            return this with
            {
                Code = code,
                Version = Version + 1,
                DateModified = DateTimeOffset.Now,
            };
        }
    }

    public enum EvaluationInterval
    {
        Disabled,
        OneHour,
        SixHours,
        OneDay,
    }
}
