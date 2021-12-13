using System;
using System.Text.Json.Serialization;
// ReSharper disable ClassNeverInstantiated.Global

namespace BlackWatch.WebApp.Features.Api;

public class Tally
{
    public string? TallySourceId { get; set; }

    public int TallySourceVersion { get; set; }

    public DateTimeOffset DateCreated { get; set; }

    public TallyState State { get; set; }

    public string? Result { get; set; }
}

public enum TallyState
{
    Indeterminate,
    NonSignalled,
    Signalled,
    Error
}

public class TallySource
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? Message { get; set; }

    public string? Code { get; set; }

    public int Version { get; set; }

    public DateTimeOffset DateModified { get; set; }

    public EvaluationInterval Interval { get; set; }

    [JsonIgnore]
    public bool IsNew => Id == null;
}

public enum EvaluationInterval
{
    Disabled,
    OneHour,
    SixHours,
    OneDay
}

public record PutTallySourceCommand(
    string Name,
    string Message,
    string Code,
    EvaluationInterval Interval);
