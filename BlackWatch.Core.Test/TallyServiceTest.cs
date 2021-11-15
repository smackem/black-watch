using System;
using System.Collections.Generic;
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

        public TallyServiceTest(ITestOutputHelper @out)
        {
            _out = @out;
        }
        
        [Fact]
        public async void EvaluateAsync()
        {
            var tally = new TallyService(new DataStore(), new NullLogger<TallyService>());
            //var x = await tally.EvaluateAsync();
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
            public Task<Tracker[]> GetTrackersAsync()
            {
                return Task.FromResult(new[]
                {
                    new Tracker("BTCUSD", null, null),
                });
            }
            public Task<Quote?> GetQuoteAsync(string symbol, DateTimeOffset date)
            {
                throw new NotImplementedException();
            }
            public Task<TallySource[]> GetTallySources(string userId)
            {
                throw new NotImplementedException();
            }
            public Task<string> GenerateIdAsync()
            {
                throw new NotImplementedException();
            }
            public Task InsertTrackersAsync(IEnumerable<Tracker> trackers)
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
