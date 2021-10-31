using System;

namespace BlackWatch.Core.Contracts
{
    public record Quote(
        string Symbol,
        decimal Open,
        decimal Close,
        decimal High,
        decimal Low,
        string Currency, 
        DateTimeOffset Date);
}
