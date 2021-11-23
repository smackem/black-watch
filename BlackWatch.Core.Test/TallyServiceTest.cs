using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Core.Services;
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
        public unsafe void Something()
        {
            static void Write(ITestOutputHelper @out, string s)
            {
                @out.WriteLine(s);
            }

            delegate*<ITestOutputHelper, string, void> print = &Write;
            print(_out, "hello");

            var arr = Enumerable.Range(1, 50).Select(n => n * n).ToArray();
            var span = arr.AsSpan(3 .. 10);
            fixed (int* arrayPtr = span)
            {
                var ptr = arrayPtr;
                for (var i = 0; i < span.Length; i++, ptr++)
                {
                    print(_out, $"{i}: {*ptr}");
                }
            }
        }

        [Fact]
        public void CompilerDirectives()
        {
            #if NET
            _out.WriteLine("NET");
            #endif
            #if NETCOREAPP
            _out.WriteLine("NETCOREAPP");
            #endif
            #if NET5_0
            _out.WriteLine("NET5_0");
            #endif
            #if NET5_0_OR_GREATER
            _out.WriteLine("NET5_0_OR_GREATER");
            #endif
            #if NETCOREAPP1_0_OR_GREATER
            _out.WriteLine("NETCOREAPP1_0_OR_GREATER");
            #endif
        }

        [Fact]
        public async void EvaluateEmpty()
        {
            var dataStore = new DataStore();
            var service = new TallyService(dataStore, new NullLogger<TallyService>());
            var tallies = await service.EvaluateAsync(UserId, EvaluationInterval.OneHour);
            Assert.Empty(tallies);
        }

        [Fact]
        public async void EvaluateSingleSignalledTally()
        {
            var dataStore = new DataStore(
                new TallySource("1", "return true;", 1, DateTimeOffset.UtcNow, EvaluationInterval.OneHour));

            var service = new TallyService(dataStore, new NullLogger<TallyService>());
            var tallies = await service.EvaluateAsync(UserId, EvaluationInterval.OneHour);
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
            var dataStore = new DataStore(
                new TallySource("1", "return false;", 1, DateTimeOffset.UtcNow, EvaluationInterval.OneHour));

            var service = new TallyService(dataStore, new NullLogger<TallyService>());
            var tallies = await service.EvaluateAsync(UserId, EvaluationInterval.OneHour);
            Assert.Collection(tallies,
                t =>
                {
                    Assert.Equal(TallyState.NonSignalled, t.State);
                    Assert.Null(t.Result);
                });
        }

        [Fact]
        public void JsTest()
        {
            var engine = new Engine();
            var value = engine.Evaluate(@"
(function() {
    return { state: 1, result: ""abc"" }
})();
");
            _out.WriteLine($"{value}");
        }

        private class DataStore : IDataStore
        {
            private static class Symbols
            {
                public const string BtcUsd = "BTCUSD";
                public const string EthUsd = "ETHUSD";
                public const string UniUsd = "UNIUSD";
            }

            private readonly TallySource[] _tallySources;

            public DataStore(params TallySource[] tallySources)
            {
                _tallySources = tallySources;
            }
            
            public Task<Tracker[]> GetTrackersAsync()
            {
                return Task.FromResult(new[]
                {
                    new Tracker(Symbols.BtcUsd, null, null),
                    new Tracker(Symbols.EthUsd, null, null),
                    new Tracker(Symbols.UniUsd, null, null),
                });
            }

            public Task<Quote?> GetQuoteAsync(string symbol, DateTimeOffset date)
            {
                var quote = symbol switch
                {
                    Symbols.BtcUsd => new Quote(symbol, 1000, 2000, 2500, 800, "USD", date),
                    Symbols.EthUsd => new Quote(symbol, 100, 200, 250, 80, "USD", date),
                    Symbols.UniUsd => new Quote(symbol, 10, 20, 25, 8, "USD", date),
                    _ => null,
                };
                return Task.FromResult(quote);
            }

            public Task<TallySource[]> GetTallySourcesAsync(string userId)
            {
                return Task.FromResult(_tallySources);
            }

            public Task<TallySource?> GetTallySourceAsync(string userId, string id)
            {
                throw new NotImplementedException();
            }

            public Task PutTallySourceAsync(string userId, TallySource tallySource)
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
            public Task SetQuoteAsync(Quote quote)
            {
                throw new NotImplementedException();
            }
            public Task<long> EnqueueJobAsync(IEnumerable<JobInfo> jobs)
            {
                throw new NotImplementedException();
            }
            public Task EnqueueJobAsync(JobInfo job)
            {
                throw new NotImplementedException();
            }
            public Task<JobInfo[]> DequeueJobsAsync(int count)
            {
                throw new NotImplementedException();
            }
            public Task<long> GetJobQueueLengthAsync()
            {
                throw new NotImplementedException();
            }
        }
    }
}
