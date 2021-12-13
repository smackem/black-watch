using System;

namespace BlackWatch.Core.Contracts;

/// <summary>
/// a tally source is an executable, versioned script that is executed against an <see cref="IDataStore"/>
/// </summary>
/// <param name="Id">the tally source id, unique in the scope of this application</param>
/// <param name="Name">the display name of the tally source</param>
/// <param name="Message">the message to be displayed for signalled tallies</param>
/// <param name="Code">the script code in JavaScript</param>
/// <param name="Version">the tally source's version, incremented everytime <see cref="TallySource.Update"/> is called</param>
/// <param name="DateModified">the point in time of the last <see cref="TallySource.Update"/> call</param>
/// <param name="Interval">the evaluation interval of this tally source</param>
public record TallySource(
    string Id,
    string Name,
    string Message,
    string Code,
    EvaluationInterval Interval,
    int Version,
    DateTimeOffset DateModified)
{
    /// <summary>
    /// creates a new version of this tally source with the same id and the specified code,
    /// with updated <see cref="Version"/> and <see cref="DateModified"/>
    /// </summary>
    public TallySource Update(string name, string message, string code, EvaluationInterval interval)
    {
        return this with
        {
            Name = name,
            Message = message,
            Code = code,
            Interval = interval,
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
