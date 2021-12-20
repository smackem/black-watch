using System;
using System.Text.RegularExpressions;
using StackExchange.Redis;

namespace BlackWatch.Core.Services;

internal static class RedisNames
{
    /// <summary>
    /// LIST{RequestInfo}
    /// </summary>
    public static RedisKey Requests(string apiTag) => new($"black-watch:requests:{apiTag}");

    /// <summary>
    /// HASH{TallySourceKey => TallySource}
    /// </summary>
    public static readonly RedisKey TallySources = new($"black-watch:tally-sources");

    /// <summary>
    /// STRING
    /// </summary>
    public static RedisValue TallySourceKey(string userId, string tallySourceId) => $"user-{userId}:{tallySourceId}";

    /// <summary>
    /// STRING
    /// </summary>
    public static RedisValue DateKey(DateTimeOffset date) => $"{date:yyyy-MM-dd}";

    /// <summary>
    /// HASH{Date => Quote}
    /// </summary>
    public static RedisKey DailyQuotes(string symbol) => $"{DailyQuotesPrefix}{symbol}";

    /// <summary>
    /// HASH{Date => Quote}
    /// </summary>
    public static RedisKey DailyQuotesRegex(string pattern) => $"{Regex.Escape(DailyQuotesPrefix)}{pattern}";

    private const string DailyQuotesPrefix = "black-watch:quotes:daily:";

    /// <summary>
    /// LIST{Quote}
    /// </summary>
    public static RedisKey HourlyQuotes(string symbol) => $"{HourlyQuotesPrefix}{symbol}";

    /// <summary>
    /// LIST{Quote}
    /// </summary>
    public static RedisKey HourlyQuotesRegex(string pattern) => $"{Regex.Escape(HourlyQuotesPrefix)}{pattern}";

    private const string HourlyQuotesPrefix = "black-watch:quotes:hourly:";

    /// <summary>
    /// LIST{Tally}
    /// </summary>
    public static RedisKey Tally(string tallySourceId) => $"black-watch:tally-{tallySourceId}";

    /// <summary>
    /// INTEGER
    /// </summary>
    public static readonly RedisKey NextId = new("black-watch:next-id");
}