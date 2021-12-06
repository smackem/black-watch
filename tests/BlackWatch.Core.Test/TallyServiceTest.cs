using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Core.Services;
using BlackWatch.Core.Test.Util;
using BlackWatch.Core.Util;
using Jint;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace BlackWatch.Core.Test
{
    public class TallyServiceTest
    {
        private readonly ITestOutputHelper _out;
        private static readonly string UserId = string.Empty;

        public TallyServiceTest(ITestOutputHelper @out)
        {
            _out = @out;
        }

        [Fact]
        public async void EvaluateEmpty()
        {
            var dataStore = new DataStore();
            var service = new TallyService(dataStore, new NullLogger<TallyService>());
            var tallies = await service.EvaluateAsync(EvaluationInterval.OneHour, UserId).ToListAsync();
            Assert.Empty(tallies);
        }

        [Fact]
        public async void EvaluateSingleSignalledTally()
        {
            var tallies = await EvaluateSingleTallySource("return true;", EvaluationInterval.OneHour);
            Assert.Collection(tallies,
                t =>
                {
                    Assert.Equal(TallyState.Signalled, t.State);
                    Assert.Null(t.Result);
                });
        }

        [Fact]
        public async void EvaluateSingleNonSignalledTally()
        {
            var tallies = await EvaluateSingleTallySource("return false;", EvaluationInterval.OneHour);
            Assert.Collection(tallies,
                t =>
                {
                    Assert.Equal(TallyState.NonSignalled, t.State);
                    Assert.Null(t.Result);
                });
        }

        [Fact]
        public async void EvaluateBtcGreaterZero()
        {
            const string source = @"
    var n = 10;
    return Daily.BTC(-n).Close > 0;
";
            var tallies = await EvaluateSingleTallySource(source, EvaluationInterval.OneHour);
            Assert.Collection(tallies,
                t =>
                {
                    Assert.Equal(TallyState.Signalled, t.State);
                    Assert.Null(t.Result);
                });
        }

        [Fact]
        public async void EvaluateEthHourly()
        {
            const string source = @"
    var n = 10;
    var eth = Hourly.ETH(-n).Close;
    return { signal: eth > 0, result: eth };
";
            var tallies = await EvaluateSingleTallySource(source, EvaluationInterval.OneHour);
            Assert.Collection(tallies,
                t =>
                {
                    Assert.Equal(TallyState.Signalled, t.State);
                    Assert.NotNull(t.Result);
                    Assert.True(int.TryParse(t.Result!, out _), "result is not integer");
                    Assert.True(int.Parse(t.Result!) > 0);
                });
        }

        [Fact]
        public async void CheckQuoteOfToday()
        {
            const string source = @"
    return { signal: true, result: Daily.BTC(0).Date };
";
            var tallies = await EvaluateSingleTallySource(source, EvaluationInterval.OneHour);
            Assert.Collection(tallies,
                t =>
                {
                    Assert.Equal(TallyState.Signalled, t.State);
                    Assert.NotNull(t.Result);
                });
        }

        [Fact]
        public async void EvaluateWithResult()
        {
            const string source = @"return { signal: true, result: 'hello' };";
            var tallies = await EvaluateSingleTallySource(source, EvaluationInterval.OneHour);
            Assert.Collection(tallies,
                t =>
                {
                    Assert.Equal(TallyState.Signalled, t.State);
                    Assert.Equal("hello", t.Result);
                });
        }

        [Fact]
        public async void EvaluateReturnedObjectWithoutResult()
        {
            const string source = @"return { signal: true };";
            var tallies = await EvaluateSingleTallySource(source, EvaluationInterval.OneHour);
            Assert.Collection(tallies,
                t =>
                {
                    Assert.Equal(TallyState.Signalled, t.State);
                    Assert.Null(t.Result);
                });
        }

        [Fact]
        public async void EvaluateInvalidReturnValue()
        {
            const string source = @"return 123;";
            var tallies = await EvaluateSingleTallySource(source, EvaluationInterval.OneHour);
            Assert.Collection(tallies,
                t =>
                {
                    Assert.Equal(TallyState.Indeterminate, t.State);
                    Assert.Equal("123", t.Result);
                });
        }

        [Fact]
        public void JsTest()
        {
            var engine = new Engine();
            var value = engine.Evaluate(@"
(function() {
    return { state: 1, result: 'abc' }
})();
");
            _out.WriteLine($"{value}");
        }

        private static async Task<IReadOnlyList<Tally>> EvaluateSingleTallySource(string source, EvaluationInterval interval)
        {
            var dataStore = new DataStore(
                new TallySource("1", source, 1, DateTimeOffset.UtcNow, interval));

            var service = new TallyService(dataStore, new NullLogger<TallyService>());
            return await service.EvaluateAsync(EvaluationInterval.OneHour, UserId).ToListAsync();
        }

        private class DataStore : IDataStore
        {
            private static class Symbols
            {
                public const string Btc = "BTC";
                public const string Eth = "ETH";
                public const string Uni = "UNI";
            }

            private readonly TallySource[] _tallySources;

            public DataStore(params TallySource[] tallySources)
            {
                _tallySources = tallySources;
            }
            
            public Task<IReadOnlyCollection<Tracker>> GetDailyTrackersAsync()
            {
                return Task.FromResult(new[]
                {
                    new Tracker(Symbols.Btc),
                    new Tracker(Symbols.Eth),
                    new Tracker(Symbols.Uni),
                } as IReadOnlyCollection<Tracker>);
            }

            public Task<IReadOnlyCollection<Tracker>> GetHourlyTrackersAsync()
            {
                return GetDailyTrackersAsync();
            }

            public Task<Quote?> GetDailyQuoteAsync(string symbol, DateTimeOffset date)
            {
                return Task.FromResult(GetQuote(symbol, date));
            }

            public Task<Quote?> GetHourlyQuoteAsync(string symbol, int hourOffset)
            {
                return Task.FromResult(GetQuote(symbol, DateTimeOffset.UtcNow));
            }

            private static Quote? GetQuote(string symbol, DateTimeOffset date)
            {
                return symbol switch
                {
                    Symbols.Btc => new Quote(symbol, Open: 1000, Close: 2000, High: 2500, Low: 800, Currency: "USD", Date: date),
                    Symbols.Eth => new Quote(symbol, Open: 100, Close: 200, High: 250, Low: 80, Currency: "USD", Date: date),
                    Symbols.Uni => new Quote(symbol, Open: 10, Close: 20, High: 25, Low: 8, Currency: "USD", Date: date),
                    _ => null,
                };
            }

            public IAsyncEnumerable<TallySource> GetTallySourcesAsync(string? userId)
            {
                return _tallySources.ToAsyncEnumerable();
            }

            public Task<TallySource?> GetTallySourceAsync(string userId, string id)
            {
                throw new NotImplementedException();
            }
            public Task PutTallySourceAsync(string userId, TallySource tallySource)
            {
                throw new NotImplementedException();
            }
            public Task<bool> DeleteTallySourceAsync(string userId, string id)
            {
                throw new NotImplementedException();
            }
            public Task PutTallyAsync(Tally tally)
            {
                throw new NotImplementedException();
            }
            public Task<Tally[]> GetTalliesAsync(string tallySourceId, int count)
            {
                throw new NotImplementedException();
            }
            public Task<string> GenerateIdAsync()
            {
                throw new NotImplementedException();
            }
            public Task PutTrackersAsync(IEnumerable<Tracker> trackers)
            {
                throw new NotImplementedException();
            }
            public Task PutDailyQuoteAsync(Quote quote)
            {
                throw new NotImplementedException();
            }
            public Task PutHourlyQuoteAsync(Quote quote)
            {
                throw new NotImplementedException();
            }
            public Task EnqueueRequestsAsync(IEnumerable<RequestInfo> jobs)
            {
                throw new NotImplementedException();
            }
            public Task EnqueueRequestAsync(RequestInfo request)
            {
                throw new NotImplementedException();
            }
            public Task<RequestInfo[]> DequeueRequestsAsync(int count, string apiTag)
            {
                throw new NotImplementedException();
            }
            public Task<long> GetRequestQueueLengthAsync(string apiTag)
            {
                throw new NotImplementedException();
            }
        }
    }
}
