using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BlackWatch.Core.Util;

public static class ValueTaskExtensions
{
    public static ConfiguredValueTaskAwaitable Return(this ValueTask task) => task.ConfigureAwait(true);

    public static ConfiguredValueTaskAwaitable Linger(this ValueTask task) => task.ConfigureAwait(false);
        
    public static ConfiguredValueTaskAwaitable<T> Return<T>(this ValueTask<T> task) => task.ConfigureAwait(true);

    public static ConfiguredValueTaskAwaitable<T> Linger<T>(this ValueTask<T> task) => task.ConfigureAwait(false);
}