using System.ComponentModel.DataAnnotations;

namespace BlackWatch.Core.Services;

public class RedisOptions
{
    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    [Range(1, 1000)]
    public int MaxTallyHistoryLength { get; set; } = 100;

    [Range(1, 100)]
    public int MaxHourlyQuotes { get; set; } = 24;
}
