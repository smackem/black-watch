using System;

namespace BlackWatch.WebApp.Features.Api
{
    public record Tally(
        string TallySourceId,
        int TallySourceVersion,
        DateTimeOffset DateCreated,
        TallyState State,
        string? Result);

    public enum TallyState
    {
        Indeterminate,
        NonSignalled,
        Signalled,
        Error,
    }

    public record TallySource(
        string Id,
        string Code,
        int Version,
        DateTimeOffset DateModified,
        EvaluationInterval Interval);

    public enum EvaluationInterval
    {
        Disabled,
        OneHour,
        SixHours,
        OneDay,
    }

    public record PutTallySourceCommand(string Code, EvaluationInterval Interval);
}