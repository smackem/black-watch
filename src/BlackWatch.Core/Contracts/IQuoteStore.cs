using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlackWatch.Core.Contracts;

public interface IQuoteStore
{
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
}
