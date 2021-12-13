using System.Collections.Generic;
using System.Threading.Tasks;
using BlackWatch.Core.Util;

namespace BlackWatch.Core.Test.Util;

public static class Extensions
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> self)
    {
        await Task.CompletedTask.Linger();// IAsyncEnumerable requires async/await

        foreach (var item in self)
        {
            yield return item;
        }
    }
}
