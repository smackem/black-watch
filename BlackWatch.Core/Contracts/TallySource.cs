using System;

namespace BlackWatch.Core.Contracts
{
    public record TallySource(
        string Code,
        int Version,
        DateTimeOffset DateModified);
}
