using System;

namespace BlackWatch.Core.Contracts
{
    public record Tally(
        string TallySourceId,
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
}
