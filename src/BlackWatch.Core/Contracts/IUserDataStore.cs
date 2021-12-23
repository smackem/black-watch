using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlackWatch.Core.Contracts;

/// <summary>
/// the central data access interface
/// </summary>
public interface IUserDataStore
{
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
    
    /// <summary>
    /// deletes all <see cref="Tally"/>s evaluated for the <see cref="TallySource"/> with the given id
    /// </summary>
    public Task PurgeTalliesAsync(string tallySourceId);
}
