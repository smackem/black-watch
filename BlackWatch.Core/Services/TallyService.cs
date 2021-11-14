using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using Jint;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Core.Services
{
    public class TallyService
    {
        private readonly IDataStore _dataStore;
        private readonly ILogger<TallyService> _logger;
        
        public TallyService(IDataStore dataStore, ILogger<TallyService> logger)
        {
            _dataStore = dataStore;
            _logger = logger;
        }

        public async Task<Tally[]> EvaluateAsync(EvaluationInterval interval)
        {
            return Array.Empty<Tally>();
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

        public async Task<object> EvaluateAsync()
        {
            var trackers = await _dataStore.GetTrackersAsync();
            var engine = new Engine();
            var obj = trackers.ToDictionary(t => t.Symbol, _ => new Func<int>(() => 100));
            engine.SetValue("X", obj);
            var value = engine.Evaluate("X.BTCUSD()");
            return value;
        }
    }
}
