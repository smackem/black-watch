using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BlackWatch.Core.Util;

public static class AsyncEnumerableExtensions
{
    public static ConfiguredCancelableAsyncEnumerable<T> Linger<T>(this IAsyncEnumerable<T> self) =>
        self.ConfigureAwait(false);

    public static ConfiguredCancelableAsyncEnumerable<T> Return<T>(this IAsyncEnumerable<T> self) =>
        self.ConfigureAwait(true);

    public static async Task<IReadOnlyList<T>> ToListAsync<T>(this IAsyncEnumerable<T> self)
    {
        var list = new List<T>();
        await foreach (var item in self.Linger())
        {
            list.Add(item);
        }
        return list;
    }
}