using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlackWatch.Core.Contracts
{
    public interface IDataStore
    {
        public Task<string> GenerateIdAsync();

        public Task InsertTrackersAsync(IEnumerable<Tracker> trackers);

        public Task<Tracker[]> GetTrackersAsync();

        public Task<Quote?> GetQuoteAsync(string symbol, DateTimeOffset date);

        public Task SetQuoteAsync(Quote quote);

        public Task<long> EnqueueJobAsync(IEnumerable<JobInfo> jobs);

        public Task EnqueueJobAsync(JobInfo job);

        public Task<JobInfo[]> DequeueJobsAsync(int count);

        public Task<long> GetJobQueueLengthAsync();

        public Task<TallySource[]> GetTallySources(string userId);
    }
}
