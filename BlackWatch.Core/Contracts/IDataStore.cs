using System;
using System.Threading.Tasks;

namespace BlackWatch.Core.Contracts
{
    public interface IDataStore
    {
        public Task<Quote?> GetQuoteAsync(string symbol, DateTimeOffset date);

        public Task SetQuoteAsync(Quote quote);
    }
}
