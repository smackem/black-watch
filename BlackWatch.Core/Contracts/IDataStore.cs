using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlackWatch.Core.Contracts
{
    /// <summary>
    /// the central data access interface
    /// </summary>
    public interface IDataStore
    {
        /// <summary>
        /// generates a new id, unique to the scope of this application
        /// </summary>
        public Task<string> GenerateIdAsync();

        /// <summary>
        /// inserts the passed trackers into the database. the primary key of the tracker
        /// is the symbol. replaces existing trackers by symbol.
        /// </summary>
        public Task PutTrackersAsync(IEnumerable<Tracker> trackers);

        /// <summary>
        /// retrieves all trackers from the database
        /// </summary>
        public Task<Tracker[]> GetTrackersAsync();

        /// <summary>
        /// gets the quote with the specified <paramref name="symbol"/> for the given <paramref name="date"/>
        /// or <c>null</c> if no matching quote found
        /// </summary>
        public Task<Quote?> GetQuoteAsync(string symbol, DateTimeOffset date);

        /// <summary>
        /// inserts the specified quote into the database, replacing an existing quote with
        /// the same symbol and date if one exists
        /// </summary>
        public Task SetQuoteAsync(Quote quote);

        /// <summary>
        /// inserts the specified <see cref="JobInfo" />s at the end of the job queue and returns
        /// the new length of the job queue
        /// </summary>
        public Task<long> EnqueueJobsAsync(IEnumerable<JobInfo> jobs);

        /// <summary>
        /// inserts the specified <see cref="JobInfo" />s at the end of the job queue and returns
        /// the new length of the job queue
        /// </summary>
        public Task<long> EnqueueJobAsync(JobInfo job);

        /// <summary>
        /// gets and removes up to <paramref name="count"/> jobs from the head of the job queue
        /// </summary>
        public Task<JobInfo[]> DequeueJobsAsync(int count);

        /// <summary>
        /// gets the current job queue length
        /// </summary>
        public Task<long> GetJobQueueLengthAsync();

        /// <summary>
        /// enumerates all <see cref="TallySource"/>s of the specified user from the database or all
        /// tally sources if <paramref name="userId"/> is <c>null</c>
        /// </summary>
        public IAsyncEnumerable<TallySource> GetTallySourcesAsync(string? userId = null);

        /// <summary>
        /// gets the user's <see cref="TallySource"/> with the specified <paramref name="id"/> or <c>null</c>
        /// if no matching tally source exists
        /// </summary>
        public Task<TallySource?> GetTallySourceAsync(string userId, string id);

        /// <summary>
        /// inserts the specified <see cref="TallySource"/> into the database, overwriting any existing tally source
        /// with the same id
        /// </summary>
        public Task PutTallySourceAsync(string userId, TallySource tallySource);

        /// <summary>
        /// removes the <see cref="TallySource"/> with the specified <paramref name="id"/> from the database and
        /// returns <c>true</c> on success or <c>false</c> if not found.
        /// also removes all <see cref="Tally"/>s generated for this tally source from the database
        /// </summary>
        public Task<bool> DeleteTallySourceAsync(string userId, string id);

        /// <summary>
        /// inserts the specified <see cref="Tally"/> into the database, appending it to the list of tallies generated
        /// for it's <see cref="TallySource"/>
        /// </summary>
        public Task PutTallyAsync(Tally tally);

        /// <summary>
        /// gets the most recent <paramref name="count"/> <see cref="Tally"/>s from the database, starting with
        /// the most recent one
        /// </summary>
        public Task<Tally[]> GetTallies(string tallySourceId, int count);
    }
}
