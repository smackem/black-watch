using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Core.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace BlackWatch.Core.Services;

/// <summary>
/// implements a <see cref="IQuoteStore"/> backed by the redis persistent cache 
/// </summary>
public class RedisQuoteStore : RedisStore, IQuoteStore
{
    private readonly ILogger<RedisQuoteStore> _logger;
    private readonly RedisOptions _options;

    public RedisQuoteStore(
        RedisConnection connection,
        ILogger<RedisQuoteStore> logger,
        IOptions<RedisOptions> options)
        : base(connection)
    {
        _logger = logger;
        _options = options.Value;
    }

    public Task<IReadOnlyCollection<Tracker>> GetDailyTrackersAsync()
    {
        return GetTrackersAsync(RedisNames.DailyQuotes("*"), RedisNames.DailyQuotesRegex(@"(\w+)"));
    }

    public Task<IReadOnlyCollection<Tracker>> GetHourlyTrackersAsync()
    {
        return GetTrackersAsync(RedisNames.HourlyQuotes("*"), RedisNames.HourlyQuotesRegex(@"(\w+)"));
    }

    public async Task<Quote?> GetDailyQuoteAsync(string symbol, DateTimeOffset date)
    {
        var db = await GetDatabaseAsync().Linger();
        var key = RedisNames.DateKey(date);
        var hash = RedisNames.DailyQuotes(symbol);
        var value = await db.HashGetAsync(hash, key).Linger();

        if (value.HasValue == false)
        {
            _logger.LogWarning("no daily quote found @{Hash}[{Date}]", hash, key);
            return null;
        }

        _logger.LogTrace("got daily quote: {Quote}", value);
        return Deserialize<Quote>(value);
    }

    public async Task RemoveDailyQuotesAsync(string symbol, DateTimeOffset threshold)
    {
        var db = await GetDatabaseAsync().Linger();
        var keys = new List<RedisValue>();
        var hash = RedisNames.DailyQuotes(symbol);
        await foreach (var entry in db.HashScanAsync(hash).Linger())
        {
            var date = DateTimeOffset.Parse((string) entry.Name, styles: DateTimeStyles.AssumeUniversal);
            if (date < threshold)
            {
                keys.Add(entry.Name);
            }
        }
        var count = await db.HashDeleteAsync(hash, keys.ToArray()).Linger();
        _logger.LogInformation("removed {Count} quotes from hash @{Key}", count, hash);
    }

    public async Task<Quote?> GetHourlyQuoteAsync(string symbol, int hourOffset)
    {
        if (hourOffset > 0)
        {
            throw new ArgumentOutOfRangeException(nameof(hourOffset), "value must be <= 0");
        }

        var index = -hourOffset;
        var db = await GetDatabaseAsync().Linger();
        var key = RedisNames.HourlyQuotes(symbol);
        var value = await db.ListGetByIndexAsync(key, index);

        if (value.HasValue == false)
        {
            _logger.LogWarning("no hourly quote found @{List}[{Index}]", key, index);
            return null;
        }

        _logger.LogTrace("got hourly quote: {Quote}", value);
        return Deserialize<Quote>(value);
    }

    public async Task PutDailyQuoteAsync(Quote quote)
    {
        var db = await GetDatabaseAsync().Linger();
        var value = Serialize(quote);
        var hash = RedisNames.DailyQuotes(quote.Symbol);
        var key = RedisNames.DateKey(quote.Date);
        await db.HashSetAsync(hash, key, value).Linger();
        _logger.LogDebug("daily quote set @{Hash}[{Date}]", hash, key);
    }

    public async Task PutHourlyQuoteAsync(Quote quote)
    {
        var db = await GetDatabaseAsync().Linger();
        var value = Serialize(quote);
        var key = RedisNames.HourlyQuotes(quote.Symbol);
        var count = await db.ListLeftPushAsync(key, value).Linger();
        _logger.LogDebug("hourly quote prepended to {Key}", key);

        var maxCount = _options.MaxHourlyQuotes;
        if (count > maxCount)
        {
            await db.ListTrimAsync(key, 0, count - 1).Linger();
            _logger.LogDebug("hourly quotes @ {Key} trimmed from {OriginalCount} to {NewCount}", key, count, maxCount);
        }
    }

    private async Task<IReadOnlyCollection<Tracker>> GetTrackersAsync(string redisPattern, string regexPattern)
    {
        var trackers = new List<Tracker>();
        var regex = new Regex(regexPattern, RegexOptions.Compiled);
        await foreach (var key in ScanKeysAsync(redisPattern))
        {
            var match = regex.Match(key);
            if (match.Success == false)
            {
                _logger.LogWarning(
                    "{Key} did not match pattern {RedisPattern} (regex {RegexPattern})",
                    key, redisPattern, regexPattern);
                continue;
            }

            var symbol = match.Groups[1].Value;
            trackers.Add(new Tracker(symbol));
        }

        return trackers;
    }
}
