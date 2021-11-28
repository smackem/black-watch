using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BlackWatch.Core.Util
{
    public static class TaskExtensions
    {
        public static ConfiguredTaskAwaitable Return(this Task task) => task.ConfigureAwait(true);

        public static ConfiguredTaskAwaitable Linger(this Task task) => task.ConfigureAwait(false);
        
        public static ConfiguredTaskAwaitable<T> Return<T>(this Task<T> task) => task.ConfigureAwait(true);

        public static ConfiguredTaskAwaitable<T> Linger<T>(this Task<T> task) => task.ConfigureAwait(false);
    }
}
