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

namespace BlackWatch.Core.Services
{
    public class TallyService
    {
        private readonly IDataStore _dataStore;
        private readonly ILogger<TallyService> _logger;

        private const string CodePrefix = "(function() {\n";
        private const string CodeSuffix = "\n})();";
        private static readonly Regex SymbolFunctionRegex = new(@"[\w]\:(\w+)", RegexOptions.Compiled);

        public TallyService(IDataStore dataStore, ILogger<TallyService> logger)
        {
            _dataStore = dataStore;
            _logger = logger;
        }

        public async IAsyncEnumerable<Tally> EvaluateAsync(EvaluationInterval interval, string? userId = null)
        {
            var ctx = await BuildContextAsync();
            var tallySources = _dataStore.GetTallySourcesAsync(userId);

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

        private async Task<IDictionary<string, Func<string, Quote?>>> BuildContextAsync()
        {
            var trackers = await _dataStore.GetTrackersAsync();
            return trackers.ToDictionary(
                t => GetFunctionName(t.Symbol),
                t => new Func<string, Quote?>(dateStr => FetchQuote(t, dateStr)));
        }

        private static string GetFunctionName(string symbol)
        {
            var match = SymbolFunctionRegex.Match(symbol);
            if (match.Success == false)
            {
                throw new ArgumentException($"symbol name {symbol} does not match expected format");
            }
            return match.Groups[1].Value;
        }

        private Quote? FetchQuote(Tracker tracker, string dateStr)
        {
            var date = ParseDateStr(dateStr);
            var quote = _dataStore.GetDailyQuoteAsync(tracker.Symbol, date).Result;
            return quote;
        }

        private static DateTimeOffset ParseDateStr(string dateStr)
        {
            return dateStr switch
            {
                var s when int.TryParse(s, out var n) => DateTimeOffset.UtcNow.AddDays(n),
                var s when DateTimeOffset.TryParse(s, out var date) => date,
                _ => throw new ArgumentException($"invalid date or date offset: {dateStr}", nameof(dateStr)),
            };
        }

        private Task<Tally> EvaluateAsync(TallySource tallySource, IDictionary<string, Func<string, Quote?>> ctx)
        {
            var engine = new Engine();
            var code = $"{CodePrefix}{tallySource.Code}{CodeSuffix}";
            engine.SetValue("X", ctx);

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

            return Task.FromResult(new Tally(tallySource.Id, tallySource.Version, DateTimeOffset.Now, state, result));
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
    }
}
