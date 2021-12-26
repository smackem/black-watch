using System;
using System.ComponentModel.DataAnnotations;

namespace BlackWatch.Core.Services;

public class TallyServiceOptions
{
    [Range(100, 100_000)]
    public long ScriptMemoryLimitKiloBytes { get; set; } = 16_000;

    [Range(10, 1000)]
    public int ScriptRecursionLimit { get; set; } = 100;

    [Range(1000, 1_000_000_000)]
    public int ScriptStatementLimit { get; set; } = 100_000;

    public TimeSpan ScriptExecutionTimeLimit { get; set; } = TimeSpan.FromSeconds(2);
}
