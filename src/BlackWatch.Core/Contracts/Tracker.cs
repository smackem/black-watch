namespace BlackWatch.Core.Contracts;

/// <summary>
/// symbol tracker as stored in the <see cref="IDataStore"/>
/// </summary>
/// <param name="Symbol"></param>
public record Tracker(string Symbol);