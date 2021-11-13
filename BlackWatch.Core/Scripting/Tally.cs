using System;
using System.Linq;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using Jint;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Core.Scripting
{
    public class Tally
    {
        private readonly string _script;
        private readonly IDataStore _dataStore;
        private readonly ILogger<Tally> _logger;
        
        public Tally(string script, IDataStore dataStore, ILogger<Tally> logger)
        {
            // black-watch:user:{user-id}:tally:{tally-source-id}
            _script = script;
            _dataStore = dataStore;
            _logger = logger;
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
