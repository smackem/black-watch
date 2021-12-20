namespace BlackWatch.Core.Contracts;

/// <summary>
/// symbol tracker as stored in the <see cref="IUserDataStore"/>
/// </summary>
/// <param name="Symbol"></param>
public record Tracker(string Symbol);