using System;
using System.Collections.Generic;

namespace BlackWatch.Core.Contracts;

/// <summary>
/// a tally produced by evaluating a <see cref="TallySource"/>
/// </summary>
/// <param name="TallySourceId">id of the <see cref="TallySource"/> that generated the tally</param>
/// <param name="TallySourceVersion">version of the <see cref="TallySource"/> that generated the tally</param>
/// <param name="DateCreated">evaluation date</param>
/// <param name="State">the tally signal state</param>
/// <param name="Result">a free-form string that conveys additional information, like a symbol</param>
/// <param name="Log">the JS log messages emitted through <c>console.log</c></param>
public record Tally(
    string TallySourceId,
    int TallySourceVersion,
    DateTimeOffset DateCreated,
    TallyState State,
    string? Result,
    IReadOnlyCollection<string> Log);

/// <summary>
/// tally states
/// </summary>
public enum TallyState
{
    /// <summary>
    /// state could not be determined
    /// </summary>
    Indeterminate,

    /// <summary>
    /// no signal
    /// </summary>
    NonSignalled,

    /// <summary>
    /// signal
    /// </summary>
    Signalled,

    /// <summary>
    /// error evaluating the <see cref="TallySource"/>
    /// </summary>
    Error,
}
