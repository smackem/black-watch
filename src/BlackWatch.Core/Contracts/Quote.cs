using System;

namespace BlackWatch.Core.Contracts
{
    /// <summary>
    /// a daily quote
    /// </summary>
    /// <param name="Symbol">the tracker symbol, like <c>X:BTCUSD</c></param>
    /// <param name="Open">opening price</param>
    /// <param name="Close">closing price</param>
    /// <param name="High">daily high</param>
    /// <param name="Low">daily low</param>
    /// <param name="Currency">the currency identifier, like <c>USD</c> or <c>EUR</c></param>
    /// <param name="Date">the date of the quote</param>
    public record Quote(
        string Symbol,
        decimal Open,
        decimal Close,
        decimal High,
        decimal Low,
        string Currency, 
        DateTimeOffset Date);
}
