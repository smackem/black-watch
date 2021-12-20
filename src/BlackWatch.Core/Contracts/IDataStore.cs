using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlackWatch.Core.Contracts;

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
    /// retrieves all trackers from the database that have a day-by-day history
    /// </summary>
    public Task<IReadOnlyCollection<Tracker>> GetDailyTrackersAsync();

    /// <summary>
    /// retrieves all trackers from the database that have an hour-by-hour history
    /// </summary>
    public Task<IReadOnlyCollection<Tracker>> GetHourlyTrackersAsync();

    /// <summary>
    /// gets the daily quote with the specified <paramref name="symbol"/> for the given <paramref name="date"/>
    /// or <c>null</c> if no matching quote found
    /// </summary>
    public Task<Quote?> GetDailyQuoteAsync(string symbol, DateTimeOffset date);

    /// <summary>
    /// removes all daily <see cref="Quote"/>s older than <paramref name="threshold"/> for the given symbol
    /// </summary>
    public Task RemoveDailyQuotesAsync(string symbol, DateTimeOffset threshold);

    /// <summary>
    /// gets the hourly quote with the specified <paramref name="symbol"/> at <c>now.AddHours(hourOffset)</c> 
    /// or <c>null</c> if no matching quote found. <paramref name="hourOffset"/> must be <c>0</c> or negative.
    /// </summary>
    public Task<Quote?> GetHourlyQuoteAsync(string symbol, int hourOffset);

    /// <summary>
    /// inserts the specified quote into the database, replacing an existing quote with
    /// the same symbol and date if one exists
    /// </summary>
    public Task PutDailyQuoteAsync(Quote quote);

    /// <summary>
    /// inserts the specified quote into the database, replacing an existing quote with
    /// the same symbol and date if one exists
    /// </summary>
    public Task PutHourlyQuoteAsync(Quote quote);

    /// <summary>
    /// inserts the specified <see cref="RequestInfo" />s in the request queues responsible for the request's
    /// api tags
    /// </summary>
    public Task EnqueueRequestsAsync(IEnumerable<RequestInfo> jobs);

    /// <summary>
    /// inserts the specified <see cref="RequestInfo" />s at the end of the job queue responsible for the
    /// request's api tag
    /// </summary>
    public Task EnqueueRequestAsync(RequestInfo request);

    /// <summary>
    /// gets and removes up to <paramref name="count"/> jobs from the head of the request queue responsible
    /// for the given <paramref name="apiTag"/>
    /// </summary>
    public Task<RequestInfo[]> DequeueRequestsAsync(int count, string apiTag);

    /// <summary>
    /// gets the current request queue length for the given <paramref name="apiTag"/>
    /// </summary>
    public Task<long> GetRequestQueueLengthAsync(string apiTag);

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
    public Task<Tally[]> GetTalliesAsync(string tallySourceId, int count);
}
