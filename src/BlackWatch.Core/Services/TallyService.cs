using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Core.Util;
using Jint;
using Jint.Native;
using Jint.Native.Object;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Core.Services;

public class TallyService
{
    private readonly IUserDataStore _userDataStore;
    private readonly IQuoteStore _quoteStore;
    private readonly ILogger<TallyService> _logger;

    private const string CodePrefix = "(function() {\n";
    private const string CodeSuffix = "\n})();";
    private static readonly Regex SymbolFunctionRegex = new(@"^\w+$", RegexOptions.Compiled);

    public TallyService(IUserDataStore userDataStore, IQuoteStore quoteStore, ILogger<TallyService> logger)
    {
        _userDataStore = userDataStore;
        _quoteStore = quoteStore;
        _logger = logger;
    }

    public async IAsyncEnumerable<Tally> EvaluateAsync(EvaluationInterval interval, string? userId = null)
    {
        var ctx = await BuildContextAsync();
        var tallySources = _userDataStore.GetTallySourcesAsync(userId);

        await foreach (var tallySource in tallySources.Linger())
        {
            if (tallySource.Interval != interval)
            {
                continue;
            }

            var tally = await EvaluateAsync(tallySource, ctx);
            yield return tally;
        }
    }

    public async Task<Tally> EvaluateAsync(TallySource tallySource)
    {
        var ctx = await BuildContextAsync();
        return await EvaluateAsync(tallySource, ctx);
    }

    private async Task<EvaluationContext> BuildContextAsync()
    {
        var dailyTrackers = await _quoteStore.GetDailyTrackersAsync();
        var daily = dailyTrackers.ToDictionary(
            t => GetFunctionName(t.Symbol),
            t => new Func<string, Quote?>(dateStr => FetchDailyQuote(t, dateStr)));

        var hourlyTrackers = await _quoteStore.GetHourlyTrackersAsync();
        var hourly = hourlyTrackers.ToDictionary(
            t => GetFunctionName(t.Symbol),
            t => new Func<string, Quote?>(offsetStr => FetchHourlyQuote(t, offsetStr)));

        return new EvaluationContext(daily, hourly);
    }

    private static string GetFunctionName(string symbol)
    {
        var match = SymbolFunctionRegex.Match(symbol);
        if (match.Success == false)
        {
            throw new ArgumentException($"symbol name {symbol} does not match expected format");
        }

        return symbol;
    }

    private Quote? FetchDailyQuote(Tracker tracker, string dateStr)
    {
        var date = ParseDateStr(dateStr);
        var quote = _quoteStore.GetDailyQuoteAsync(tracker.Symbol, date).Result;
        return quote;
    }

    private static DateTimeOffset ParseDateStr(string dateStr)
    {
        return dateStr switch
        {
            null or "" or "last" => DateTimeOffset.UtcNow.AddDays(-1),
            var s when int.TryParse(s, out var n) => DateTimeOffset.UtcNow.AddDays(n - 1),
            var s when DateTimeOffset.TryParse(s, out var date) => date,
            _ => throw new ArgumentException($"invalid date or date offset: {dateStr}", nameof(dateStr)),
        };
    }

    private Quote? FetchHourlyQuote(Tracker tracker, string offsetStr)
    {
        var offset = ParseOffsetStr(offsetStr);
        var quote = _quoteStore.GetHourlyQuoteAsync(tracker.Symbol, offset).Result;
        return quote;
    }

    private static int ParseOffsetStr(string offsetStr)
    {
        return offsetStr switch
        {
            null or "" or "now" or "last" => 0,
            var s when int.TryParse(s, out var offset) => offset,
            _ => throw new ArgumentException($"invalid hour offset: {offsetStr}", nameof(offsetStr)),
        };
    }

    private Task<Tally> EvaluateAsync(TallySource tallySource, EvaluationContext ctx)
    {
        var code = $"{CodePrefix}{tallySource.Code}{CodeSuffix}";
        var console = new JsConsole
        {
            Logger = _logger,
        };

        var engine = new Engine(options =>
            {
                options.LimitMemory(16 * 1024 * 1024); // MB
                options.LimitRecursion(100);
                options.MaxStatements(100_000);
                options.TimeoutInterval(TimeSpan.FromSeconds(2));
            })
            .SetValue("Daily", ctx.DailyQuotes)
            .SetValue("Hourly", ctx.HourlyQuotes)
            .SetValue("console", console);

        JsValue? value;
        string? errorMessage = null;
        try
        {
            value = engine.Evaluate(code);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "error evaluating javascript {Code}", code);
            value = null;
            errorMessage = e.Message;
        }

        var (state, result) = value switch
        {
            null => (TallyState.Error, errorMessage),
            JsBoolean b when b == JsBoolean.True => (TallyState.Signalled, null),
            JsBoolean b when b == JsBoolean.False => (TallyState.NonSignalled, null),
            ObjectInstance obj => DecodeReturnedObject(obj),
            _ => (TallyState.Indeterminate, value.ToString()),
        };

        var tally = new Tally(
            TallySourceId: tallySource.Id,
            TallySourceVersion: tallySource.Version,
            DateCreated: DateTimeOffset.Now,
            State: state,
            Result: result,
            Log: console.LogMessages);
        return Task.FromResult(tally);
    }

    private static (TallyState signal, string? result) DecodeReturnedObject(ObjectInstance obj)
    {
        if (obj.TryGetValue("signal", out var signalVal) == false)
        {
            return (TallyState.Indeterminate, obj.ToString());
        }

        TallyState signal;

        switch (signalVal)
        {
            case JsBoolean b when b == JsBoolean.True:
                signal = TallyState.Signalled;
                break;
            case JsBoolean b when b == JsBoolean.False:
                signal = TallyState.NonSignalled;
                break;
            default:
                return (TallyState.Indeterminate, obj.ToString());
        }

        var result = obj.TryGetValue("result", out var resultVal)
            ? resultVal.ToString()
            : null;

        return (signal, result);
    }

    private record EvaluationContext(
        IDictionary<string, Func<string, Quote?>> DailyQuotes,
        IDictionary<string, Func<string, Quote?>> HourlyQuotes);

    private class JsConsole
    {
        private readonly LinkedList<string> _log = new();

        public IReadOnlyCollection<string> LogMessages => _log.ToArray();

        public ILogger? Logger { get; init; }

        // ReSharper disable once InconsistentNaming
        // ReSharper disable once MemberCanBePrivate.Local
        public void assert(bool condition, string? data)
        {
            if (condition)
            {
                return;
            }

            var message = data != null
                ? "assertion failed: " + data
                : "assertion failed";
            log(message);
            throw new Exception(message);
        }

        // ReSharper disable once InconsistentNaming
        public void assert(bool condition)
        {
            // ReSharper disable once IntroduceOptionalParameters.Local
            assert(condition, null);
        }

        // ReSharper disable once MemberCanBePrivate.Local
        // ReSharper disable once InconsistentNaming
        public void log(string s)
        {
            Logger?.LogInformation("JsConsole: {JsLogMessage}", s);

            _log.AddLast(s);

            if (_log.Count > 100)
            {
                _log.RemoveFirst();
            }
        }
    }
}