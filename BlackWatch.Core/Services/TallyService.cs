using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using Jint;
using Jint.Native;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Core.Services
{
    public class TallyService
    {
        private readonly IDataStore _dataStore;
        private readonly ILogger<TallyService> _logger;
        private const string CodePrefix = "(function() {\n";
        private const string CodeSuffix = "\n})();";

        public TallyService(IDataStore dataStore, ILogger<TallyService> logger)
        {
            _dataStore = dataStore;
            _logger = logger;
        }

        public async Task<Tally[]> EvaluateAsync(EvaluationInterval interval)
        {
            var ctx = await BuildContextAsync();
            var tallySources = await _dataStore.GetTallySources("0");
            var tallies = new List<Tally>();
            foreach (var tallySource in tallySources)
            {
                var tally = await EvaluateAsync(tallySource, ctx);
                tallies.Add(tally);
            }

            return tallies.ToArray();
        }

        private async Task<IDictionary<string, Func<string, Quote?>>> BuildContextAsync()
        {
            var trackers = await _dataStore.GetTrackersAsync();
            return trackers.ToDictionary(
                t => t.Symbol,
                t => new Func<string, Quote?>(dateStr => FetchQuote(t, dateStr)));
        }

        private Quote? FetchQuote(Tracker tracker, string dateStr)
        {
            var date = ParseDateStr(dateStr);
            var quote = _dataStore.GetQuoteAsync(tracker.Symbol, date).Result;
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

        public Task<Tally> EvaluateAsync(TallySource tallySource, IDictionary<string, Func<string, Quote?>> ctx)
        {
            var engine = new Engine();
            var code = $"{CodePrefix}{tallySource.Code}{CodeSuffix}";
            engine.SetValue("X", ctx);

            JsValue? value;
            try
            {
                value = engine.Evaluate(code);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "error evaluating javascript {Code}", code);
                value = null;
            }

            var state = value switch
            {
                null => TallyState.Error,
                JsBoolean b when b == JsBoolean.True => TallyState.Signalled,
                JsBoolean b when b == JsBoolean.False => TallyState.NonSignalled,
                _ => TallyState.Indeterminate,
            };

            return Task.FromResult(new Tally(tallySource.Id, DateTimeOffset.Now, state, value?.ToString()));
        }
    }
}
