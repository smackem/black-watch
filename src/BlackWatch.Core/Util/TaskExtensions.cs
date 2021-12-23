using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace BlackWatch.Core.Util;

public static class TaskExtensions
{
    public static ConfiguredTaskAwaitable Return(this Task task) => task.ConfigureAwait(true);

    public static ConfiguredTaskAwaitable Linger(this Task task) => task.ConfigureAwait(false);

    public static ConfiguredTaskAwaitable<T> Return<T>(this Task<T> task) => task.ConfigureAwait(true);

    public static ConfiguredTaskAwaitable<T> Linger<T>(this Task<T> task) => task.ConfigureAwait(false);

    public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout)
    {
        using var delayCancellation = new CancellationTokenSource();
        var delay = Task.Delay(timeout, delayCancellation.Token);
        var completedTask = await Task.WhenAny(task, delay);
        if (completedTask == delay)
        {
            throw new TimeoutException("The operation has timed out.");
        }

        delayCancellation.Cancel();
        return await task; // very important in order to propagate exceptions
    }
}